using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Models.ViewModels.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentAppMvc.Services.Stores;

namespace AppointmentAppMvc.Controllers.AdminControllers
{
    [Authorize(Roles = "Admin")]
    public class ClientBaseController : Controller
    {
        private readonly IServiceManager _service;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<UserModel> _userManager;
        private readonly IStoreContext _storeContext;

        public ClientBaseController(IServiceManager serviceManager, IAppointmentService appointmentService,
            UserManager<UserModel> userManager, IStoreContext storeContext)
        {
            _service = serviceManager;
            _appointmentService = appointmentService;
            _userManager = userManager;
            _storeContext = storeContext;
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
                var store = await _storeContext.GetCurrentStoreAsync();
                var user = string.IsNullOrEmpty(appointment.ClientId) || store == null
                    ? null
                    : await _userManager.Users
                        .SingleOrDefaultAsync(candidate => candidate.Id == appointment.ClientId && candidate.StoreId == store.Id);

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
