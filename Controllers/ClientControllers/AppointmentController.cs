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

namespace AppointmentAppMvc.Controllers.ClientControllers
{
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appService;
        private readonly IServiceManager _serviceManager;
        private readonly IWorkScheduleService _workSchedule;
        private readonly UserManager<UserModel> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AppointmentController(IAppointmentService appservice,
            IServiceManager serviceManager,
            IWorkScheduleService workschedule,
            IHubContext<NotificationHub> hubContext,
            UserManager<UserModel> userManager)

        {
            _appService = appservice;
            _serviceManager = serviceManager;
            _workSchedule = workschedule;
            _hubContext = hubContext;
            _userManager = userManager;
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
            var isDayOpenList = new List<bool>();

            // Filtrar os schedules usando a data correta do fuso alvo
            var schedulesConfigurados = workSchedules
                .Where(ws => ws.Day.Date >= todayInTargetZone)
                .OrderBy(ws => ws.Day.Date)
                .ToList();

            foreach (var workSchedule in schedulesConfigurados)
            {
                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    appointments.ToList(),
                    allServices
                );

                if (workSchedule.Day.Date == todayInTargetZone)
                {
                    // Aplica a margem de 20 minutos baseada no horário local do alvo
                    var margem = nowInTargetZone.TimeOfDay.Add(TimeSpan.FromMinutes(20));
                    hoursInDay = hoursInDay.Where(h => h > margem).ToList();
                }

                appointmentDateAndHourList[workSchedule.Day.Date] = hoursInDay;
                isDayOpenList.Add(workSchedule.IsSelected);
            }

            // ... Resto do seu código para buscar colaboradores (Workers) ...
            var usersWithRoles = await _userManager.GetUsersInRoleAsync("Colaborador");
            var workersModel = usersWithRoles.Select(item => new UserWithRolesViewModel
            {
                Id = item.Id,
                Name = item.Name
            }).ToList();

            return new AppointmentViewModel
            {
                Services = allServices,
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = isDayOpenList,
                Workers = workersModel
            };
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            var appointmentsViewModel = await LoadAppointmentViewModelAsync();
            return View(appointmentsViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AppointmentViewModel model)
        {
            if (string.IsNullOrEmpty(model.ClientId))
                return BadRequest("Client ID não encontrado");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID não encontrado");

            if (model.SelectedServiceId == null)
                return BadRequest("Nenhum serviço foi selecionado.");

            if (model.SelectedDayAndHour == default)
                return BadRequest("Nenhuma data e horário foram selecionados.");

            try
            {
                var selectedService = await _serviceManager.GetByIdAsync(model.SelectedServiceId);
                if (selectedService == null)
                {
                    return BadRequest("Serviço não encontrado no banco de dados.");
                }

                var newAppointment = new AppointmentModel(Guid.NewGuid(), userId, model.SelectedServiceId, model.WorkerId, model.SelectedDayAndHour, "Waiting");

                await _appService.Create(newAppointment);
                TempData["SuccessMessage"] = "Agendamento realizado com sucesso!";
                await _hubContext.Clients.All.SendAsync("ReceiveNewAppointment", "Novo agendamento realizado!");
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
        public async Task<IActionResult> GetAvailableHours(string workerId, int offsetWeeks = 0, string userTimeZoneId = "E. South America Standard Time")
        {
            if (string.IsNullOrEmpty(workerId))
                return BadRequest("Colaborador não especificado.");

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
                .Where(ws => ws.WorkerId.ToString() == workerId)
                .Where(ws => ws.Day.Date >= todayInTargetZone) // Corrigido para usar o fuso correto
                .OrderBy(ws => ws.Day.Date)
                .ToList();

            var appointmentsDoTrabalhador = appointments
                .Where(ap => ap.WorkerId == workerId)
                .ToList();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenList = new List<bool>();

            foreach (var workSchedule in schedulesDoTrabalhador)
            {
                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    appointmentsDoTrabalhador,
                    allServices.ToList()
                );

                if (workSchedule.Day.Date == todayInTargetZone)
                {
                    // Margem usando o horário local do usuário e não do servidor de hospedagem
                    var margem = nowInTargetZone.TimeOfDay.Add(TimeSpan.FromMinutes(20));
                    hoursInDay = hoursInDay.Where(h => h > margem).ToList();
                }

                appointmentDateAndHourList[workSchedule.Day.Date] = hoursInDay;
                isDayOpenList.Add(workSchedule.IsSelected);
            }

            var viewModel = new AppointmentViewModel
            {
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = isDayOpenList
            };

            return PartialView("_HoursTablePartial", viewModel);
        }
    }
}
