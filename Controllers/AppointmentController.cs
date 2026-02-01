using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using appointmentApp.Services.Utilities;
using appointmentApp.Services.WorkScheduleService;
using AppointmentAppMvc.Hubs;
using AppointmentAppMvc.Models.ViewModels;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Services.Utilities.Enum;
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
        private readonly IHubContext<NotificationHub> _hubContext;

        public AppointmentController(IAppointmentService appservice,
            IServiceManager serviceManager,
            IWorkScheduleService workschedule,
            IHubContext<NotificationHub> hubContext)

        {
            _appService = appservice;
            _serviceManager = serviceManager;
            _workSchedule = workschedule;
            _hubContext = hubContext;
        }

        private async Task<AppointmentViewModel> LoadAppointmentViewModelAsync()
        {
            var services = await _serviceManager.GetAsync("");
            var workSchedules = await _workSchedule.GetAsync();
            var appointments = await _appService.Get();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenList = new List<bool>();
            var servicesIdsInAppointment = appointments.Select(x => x.ServiceId);
            var servicesInDbList = new List<ServiceModel>();
            foreach (var serviceId in servicesIdsInAppointment)
            {

                var servicesInAppointment = await _serviceManager.GetByIdAsync(serviceId);
                servicesInDbList.Add(servicesInAppointment);

            }

            foreach (var workSchedule in workSchedules)
            {
                isDayOpenList.Add(workSchedule.IsSelected);

                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    appointments.ToList(),
                    servicesInDbList
                );

                appointmentDateAndHourList[workSchedule.Day] = hoursInDay;
            }

            return new AppointmentViewModel
            {
                Services = services.ToList(),
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = isDayOpenList
            };
        }

        [HttpGet]
        public async Task<ActionResult> Index()
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

                var newAppointment = new AppointmentModel(Guid.NewGuid(), userId, model.SelectedServiceId, model.SelectedDayAndHour, "Waiting");

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
