using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using appointmentApp.Services.Utilities;
using appointmentApp.Services.WorkScheduleService;
using AppointmentAppMvc.Hubs;
using AppointmentAppMvc.Models.ViewModels;
using AppointmentAppMvc.Models.ViewModels.Account;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Services.Utilities.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using AppointmentAppMvc.Services.Stores;
using appointmentApp.Constants;

namespace AppointmentAppMvc.Controllers.ClientControllers
{
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appService;
        private readonly IServiceManager _serviceManager;
        private readonly IWorkScheduleService _workSchedule;
        private readonly UserManager<UserModel> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IStoreContext _storeContext;

        public AppointmentController(IAppointmentService appservice,
            IServiceManager serviceManager,
            IWorkScheduleService workschedule,
            IHubContext<NotificationHub> hubContext,
            UserManager<UserModel> userManager,
            IStoreContext storeContext)

        {
            _appService = appservice;
            _serviceManager = serviceManager;
            _workSchedule = workschedule;
            _hubContext = hubContext;
            _userManager = userManager;
            _storeContext = storeContext;
        }

        private async Task<string?> SelectAvailableWorkerAsync(
            DateTime appointmentDay,
            TimeSpan serviceDuration,
            string? preferredWorkerId,
            IEnumerable<WorkScheduleModel> workSchedules,
            IEnumerable<AppointmentModel> appointments,
            IEnumerable<ServiceModel> services)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null)
            {
                return null;
            }

            var workers = (await _userManager.GetUsersInRoleAsync(Roles.Worker))
                .Where(worker => worker.StoreId == store.Id)
                .ToList();
            var candidates = workers
                .Where(worker => string.IsNullOrEmpty(preferredWorkerId) || worker.Id == preferredWorkerId)
                .Where(worker => workSchedules.Any(schedule =>
                    schedule.WorkerId?.ToString() == worker.Id &&
                    schedule.Day.Date == appointmentDay.Date &&
                    schedule.IsSelected &&
                    schedule.Start <= appointmentDay.TimeOfDay &&
                    schedule.End >= appointmentDay.TimeOfDay.Add(serviceDuration)))
                .Where(worker => !appointments.Any(appointment =>
                    appointment.WorkerId == worker.Id &&
                    appointment.Status != "Canceled" &&
                    appointment.Status != "Finished" &&
                    appointment.Day.Date == appointmentDay.Date &&
                    appointment.Day < appointmentDay.Add(serviceDuration) &&
                    appointmentDay < appointment.Day.Add(GetServiceDuration(appointment.ServiceId))))
                .Select(worker => new
                {
                    worker.Id,
                    ScheduledAppointments = appointments.Count(appointment =>
                        appointment.WorkerId == worker.Id &&
                        appointment.Status != "Canceled"),
                    FinishedAppointments = appointments.Count(appointment =>
                        appointment.WorkerId == worker.Id && appointment.Status == "Finished")
                })
                .ToList();

            if (candidates.Count == 0)
                return null;

            var totalWeight = candidates.Sum(candidate =>
                1d / (1 + candidate.ScheduledAppointments));
            var randomValue = Random.Shared.NextDouble() * totalWeight;

            foreach (var candidate in candidates)
            {
                randomValue -= 1d / (1 + candidate.ScheduledAppointments);
                if (randomValue <= 0)
                    return candidate.Id;
            }

            return candidates[^1].Id;

            TimeSpan GetServiceDuration(int serviceId)
            {
                return services.FirstOrDefault(service => service.ServiceId == serviceId)?.Duration
                    ?? TimeSpan.FromMinutes(30);
            }
        }

        private async Task<AppointmentViewModel> LoadAppointmentViewModelAsync(int offsetWeeks = 0, string userTimeZoneId = "E. South America Standard Time")
        {
            // 1. Tenta encontrar o fuso passado (ex: "Eastern Standard Time" para Nova York)
            TimeZoneInfo targetTimeZone;
            try
            {
                targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            }
            catch
            {
                // Fallback caso venha um ID inválido
                targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }

            // 2. Calcula o "Agora" e o "Hoje" baseado no fuso de quem está agendando
            DateTime nowInTargetZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, targetTimeZone);
            DateTime todayInTargetZone = nowInTargetZone.Date;

            DateTime startDate = todayInTargetZone.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);

            // Buscas
            var allServices = (await _serviceManager.GetAsync("")).ToList();
            var workSchedules = await _workSchedule.GetAsync(startDate, endDate);
            var appointments = await _appService.Get();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenByDate = new Dictionary<DateTime, bool>();

            // Filtrar os schedules usando a data correta do fuso alvo
            var schedulesConfigurados = workSchedules
                .Where(ws => ws.Day.Date >= todayInTargetZone)
                .OrderBy(ws => ws.Day.Date)
                .ToList();

            foreach (var workSchedule in schedulesConfigurados)
            {
                var workerId = workSchedule.WorkerId?.ToString();
                var workerAppointments = string.IsNullOrEmpty(workerId)
                    ? appointments.ToList()
                    : appointments.Where(appointment => appointment.WorkerId == workerId).ToList();
                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    workerAppointments,
                    allServices
                );

                if (workSchedule.Day.Date == todayInTargetZone)
                {
                    // Aplica a margem de 20 minutos baseada no horário local do alvo
                    var margem = nowInTargetZone.TimeOfDay.Add(TimeSpan.FromMinutes(20));
                    hoursInDay = hoursInDay.Where(h => h > margem).ToList();
                }

                appointmentDateAndHourList[workSchedule.Day.Date] =
                    appointmentDateAndHourList.TryGetValue(workSchedule.Day.Date, out var existingHours)
                        ? existingHours.Union(hoursInDay).OrderBy(hour => hour).ToList()
                        : hoursInDay;
                isDayOpenByDate[workSchedule.Day.Date] =
                    isDayOpenByDate.GetValueOrDefault(workSchedule.Day.Date) || workSchedule.IsSelected;
            }


            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null)
            {
                return new AppointmentViewModel
                {
                    Services = allServices,
                    HoursToSchedule = appointmentDateAndHourList,
                    Workers = new List<UserWithRolesViewModel>()
                };
            }

            var usersWithRoles = (await _userManager.GetUsersInRoleAsync(Roles.Worker))
                .Where(worker => worker.StoreId == store.Id);
            var workersModel = usersWithRoles.Select(item => new UserWithRolesViewModel
            {
                Id = item.Id,
                Name = item.Name
            }).ToList();

            return new AppointmentViewModel
            {
                Services = allServices,
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = appointmentDateAndHourList.Keys
                    .Select(date => isDayOpenByDate.GetValueOrDefault(date))
                    .ToList(),
                Workers = workersModel
            };
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            if (await _storeContext.GetCurrentStoreAsync() == null)
            {
                return NotFound("Loja não encontrada.");
            }

            var appointmentsViewModel = await LoadAppointmentViewModelAsync();
            return View(appointmentsViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AppointmentViewModel model)
        {
            if (await _storeContext.GetCurrentStoreAsync() == null)
            {
                return NotFound("Loja não encontrada.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                if (string.IsNullOrWhiteSpace(model.GuestName))
                    ModelState.AddModelError(nameof(model.GuestName), "Informe seu nome.");

                if (string.IsNullOrWhiteSpace(model.GuestPhone))
                    ModelState.AddModelError(nameof(model.GuestPhone), "Informe seu telefone.");
            }

            if (model.SelectedServiceId == null)
                return BadRequest("Nenhum serviço foi selecionado.");

            if (model.SelectedDayAndHour == default)
                return BadRequest("Nenhuma data e horário foram selecionados.");

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await LoadAppointmentViewModelAsync();
                invalidViewModel.GuestName = model.GuestName;
                invalidViewModel.GuestPhone = model.GuestPhone;
                return View(invalidViewModel);
            }

            try
            {
                var selectedService = await _serviceManager.GetByIdAsync(model.SelectedServiceId);
                if (selectedService == null)
                {
                    return BadRequest("Service not Found.");
                }

                var workSchedules = await _workSchedule.GetAsync(
                    model.SelectedDayAndHour.Date,
                    model.SelectedDayAndHour.Date.AddDays(1).AddTicks(-1));
                var appointments = await _appService.Get();
                var allServices = await _serviceManager.GetAsync("");
                var assignedWorkerId = await SelectAvailableWorkerAsync(
                    model.SelectedDayAndHour,
                    selectedService.Duration,
                    model.WorkerId,
                    workSchedules,
                    appointments,
                    allServices);

                if (assignedWorkerId == null)
                {
                    ModelState.AddModelError("", "Não há profissional disponível para o horário selecionado.");
                    var unavailableViewModel = await LoadAppointmentViewModelAsync();
                    return View(unavailableViewModel);
                }

                string serviceName = selectedService.ServiceName;
                double servicePrice = selectedService.Price;
                var newAppointment = new AppointmentModel(
                    Guid.NewGuid(),
                    userId,
                    model.SelectedServiceId,
                    serviceName,
                    servicePrice,
                    assignedWorkerId,
                    model.SelectedDayAndHour,
                    "Waiting",
                    userId == null ? model.GuestName!.Trim() : null,
                    userId == null ? UserModel.NormalizePhoneNumber(model.GuestPhone!) : null);

                var store = await _storeContext.GetCurrentStoreAsync();
                if (store == null)
                {
                    ModelState.AddModelError(string.Empty, "Nenhuma loja ativa foi configurada.");
                    return View(await LoadAppointmentViewModelAsync());
                }
                newAppointment.SetStore(store.Id);

                await _appService.Create(newAppointment);
                TempData["SuccessMessage"] = "Agendamento realizado com sucesso!";
                await _hubContext.Clients.User(assignedWorkerId)
                    .SendAsync("ReceiveNewAppointment", "Novo agendamento realizado!");
                ViewBag.NewAppointment = "New";
                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", $"Erro ao criar agendamento: {ex.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocorreu um erro inesperado ao tentar criar o agendamento.");
            }

            // Se falhar, recarrega a view com os dados necessários
            var appointmentsViewModel = await LoadAppointmentViewModelAsync();
            return View(appointmentsViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Boa prática de segurança
        public async Task<ActionResult> UpdateStatus(Guid id, Status newStatus)
        {
            var dbappointment = await _appService.GetById(id);

            if (dbappointment == null)
                return NotFound("Este agendamento não existe.");

            // Atualiza o status no objeto da entidade
            dbappointment.SetStatus(newStatus);

            // PERSISTÊNCIA: Você precisa chamar o serviço para salvar no banco!
            await _appService.Update(dbappointment);

            TempData["SuccessMessage"] = "Status atualizado com sucesso!";
            string refererUrl = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl); // Redireciona exatamente para a mesma URL atual
            }
            // Redireciona para onde o usuário estava (ex: IndexAdm da Home)
            return View();
        }
        // GET: AppointmentController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        public async Task<ActionResult> CancelAppointment(Guid id, Status newStatus)
        {
            var dbappointment = await _appService.GetById(id);

            if (dbappointment == null)
                return NotFound("Este agendamento não existe.");

            // Atualiza o status no objeto da entidade
            dbappointment.SetStatus(Status.Canceled);

            // PERSISTÊNCIA: Você precisa chamar o serviço para salvar no banco!
            await _appService.Update(dbappointment);

            TempData["SuccessMessage"] = "Status atualizado com sucesso!";

            // Redireciona para onde o usuário estava (ex: IndexAdm da Home)
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Delete(Guid id)
        {

            var appointment = await _appService.GetById(id);
            if (appointment == null)
            {
                return BadRequest("Id not Existis");
            }
            await _appService.Delete(appointment);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (userRoles.Contains("Admin"))
                return RedirectToAction("IndexAdm", "Home");
            else
                return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestAppointmentId()
        {
            var lastApp = (await _appService.Get()).OrderByDescending(x => x.Day).FirstOrDefault();
            return Json(new { id = lastApp?.Id, client = lastApp?.ClientId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableHours(string? workerId, int offsetWeeks = 0, string userTimeZoneId = "E. South America Standard Time")
        {
            // Define o fuso horário correto para a busca
            TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            DateTime nowInTargetZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, targetTimeZone);
            DateTime todayInTargetZone = nowInTargetZone.Date;

            DateTime startDate = todayInTargetZone.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);

            var allServices = await _serviceManager.GetAsync("");
            var workSchedules = await _workSchedule.GetAsync(startDate, endDate);
            var appointments = await _appService.Get();

            var schedulesDoTrabalhador = workSchedules
                .Where(ws => string.IsNullOrEmpty(workerId) || ws.WorkerId.ToString() == workerId)
                .Where(ws => ws.Day.Date >= todayInTargetZone) // Corrigido para usar o fuso correto
                .OrderBy(ws => ws.Day.Date)
                .ToList();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenByDate = new Dictionary<DateTime, bool>();

            foreach (var workSchedule in schedulesDoTrabalhador)
            {
                var scheduleWorkerId = workSchedule.WorkerId?.ToString();
                var workerAppointments = string.IsNullOrEmpty(scheduleWorkerId)
                    ? appointments.ToList()
                    : appointments.Where(appointment => appointment.WorkerId == scheduleWorkerId).ToList();
                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    workerAppointments,
                    allServices.ToList()
                );

                if (workSchedule.Day.Date == todayInTargetZone)
                {
                    // Margem usando o horário local do usuário e não do servidor de hospedagem
                    var margem = nowInTargetZone.TimeOfDay.Add(TimeSpan.FromMinutes(20));
                    hoursInDay = hoursInDay.Where(h => h > margem).ToList();
                }

                appointmentDateAndHourList[workSchedule.Day.Date] =
                    appointmentDateAndHourList.TryGetValue(workSchedule.Day.Date, out var existingHours)
                        ? existingHours.Union(hoursInDay).OrderBy(hour => hour).ToList()
                        : hoursInDay;
                isDayOpenByDate[workSchedule.Day.Date] =
                    isDayOpenByDate.GetValueOrDefault(workSchedule.Day.Date) || workSchedule.IsSelected;
            }

            var viewModel = new AppointmentViewModel
            {
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = appointmentDateAndHourList.Keys
                    .Select(date => isDayOpenByDate.GetValueOrDefault(date))
                    .ToList()
            };

            return PartialView("_HoursTablePartial", viewModel);
        }
    }
}
