using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using appointmentApp.Services.Utilities;
using appointmentApp.Services.WorkScheduleService;
using AppointmentAppMvc.Hubs;
using AppointmentAppMvc.Models.ViewModels;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Services.Utilities.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace appointmentApp.Controllers
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

        private async Task<AppointmentViewModel> LoadAppointmentViewModelAsync()
        {
            // 1. Buscas
            var allServices = (await _serviceManager.GetAsync("")).ToList();
            var workSchedules = await _workSchedule.GetAsync(); // Aqui vêm as configs de Dom a Dom
            var appointments = await _appService.Get();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenList = new List<bool>();

            // 2. Ordenar por data e garantir que pegamos tudo do banco (Hoje em diante)
            var schedulesConfigurados = workSchedules
                .Where(ws => ws.Day.Date >= DateTime.Today)
                .OrderBy(ws => ws.Day.Date)
                .ToList(); // Se tiver 7, 15 ou 30 dias no banco, ele pegará todos

            foreach (var workSchedule in schedulesConfigurados)
            {
                // Gerar os horários para o dia (seja ele sábado, domingo ou segunda)
                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    appointments.ToList(),
                    allServices
                );

                // Filtro para o dia de hoje (não mostrar horários que já passaram)
                if (workSchedule.Day.Date == DateTime.Today)
                {
                    var margem = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(20));
                    hoursInDay = hoursInDay.Where(h => h > margem).ToList();
                }

                // ADICIONA AO DICIONÁRIO (Independente do dia da semana)
                appointmentDateAndHourList[workSchedule.Day.Date] = hoursInDay;

                // Sincroniza se o dia está aberto ou fechado conforme o seu Checkbox no Admin
                isDayOpenList.Add(workSchedule.IsSelected);
            }

            var usersWithRoles = await UserModel.UserWithRolesAsync(_userManager);
            var workers = usersWithRoles.Where(w => w.Role == "Colaborador").ToList();

            return new AppointmentViewModel
            {
                Services = allServices,
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = isDayOpenList,
                Workers = workers
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

            // Redireciona para onde o usuário estava (ex: IndexAdm da Home)
            return RedirectToAction("Index", "Client");
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
    }
}
