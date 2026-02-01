using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Models.ViewModels.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace appointmentApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClientController : Controller
    {
        private readonly IServiceManager _service;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<UserModel> _userManager;

        public ClientController(IServiceManager serviceManager, IAppointmentService appointmentService,
            UserManager<UserModel> userManager)
        {
            _service = serviceManager;
            _appointmentService = appointmentService;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; 
            List<ClientIndexViewModel> clients = new List<ClientIndexViewModel>();

            var allAppointments = await _appointmentService.Get();

            if (allAppointments == null || !allAppointments.Any())
            {
                return View(clients);
            }

            var sortedAppointments = allAppointments.OrderByDescending(a => a.Day);

            int totalItems = sortedAppointments.Count();
            var pagedAppointments = sortedAppointments
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var appointment in pagedAppointments)
            {
                var findService = await _service.GetByIdAsync(appointment.ServiceId);
                var user = await _userManager.FindByIdAsync(appointment.ClientId);

                clients.Add(new ClientIndexViewModel
                {
                    Appointment = appointment,
                    Service = findService,
                    User = user
                });
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(clients);
        }
        public async Task<IActionResult> Delete(Guid id)
        {

            var appointment = await _appointmentService.GetById(id);
            if (appointment == null)
            {
                return BadRequest("Id not Existis");
            }
            await _appointmentService.Delete(appointment);
            return RedirectToAction("Index");
        }
    }
}
