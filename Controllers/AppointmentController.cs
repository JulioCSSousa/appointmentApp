using appointmentApp.Models.Entities;
using appointmentApp.Models.ViewModels;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using appointmentApp.Services.Utilities;
using appointmentApp.Services.WorkScheduleService;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace appointmentApp.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appService;
        private readonly IServiceManager _serviceManager;
        private readonly IWorkScheduleService _workSchedule;

        public AppointmentController(IAppointmentService appservice, 
            IServiceManager serviceManager, 
            IWorkScheduleService workschedule)
        {
            _appService = appservice;
            _serviceManager = serviceManager;
            _workSchedule = workschedule;
        }

        private async Task<AppointmentViewModel> LoadAppointmentViewModelAsync()
        {
            var services = await _serviceManager.GetAsync("");
            var workSchedules = await _workSchedule.GetAsync();
            var appointments = await _appService.Get();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenList = new List<bool>();
            var servicesIdsInAppointment = appointments.Select(x => x.ServiceId);
            var servicesInDbList = new List<Service>();
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

                var newAppointment = new Appointment(Guid.NewGuid(), userId, model.SelectedServiceId, model.SelectedDayAndHour);

                await _appService.Create(newAppointment);
                TempData["SuccessMessage"] = "Agendamento realizado com sucesso!";
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





        // GET: AppointmentController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }


        

        // GET: AppointmentController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AppointmentController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
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
    }
}
