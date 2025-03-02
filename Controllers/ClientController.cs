using appointmentApp.Models.Entities;
using appointmentApp.Models.Repository.AppointmentRepository;
using appointmentApp.Models.ViewModels;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace appointmentApp.Controllers
{ 
    public class ClientController : Controller
    {
        private readonly IServiceManager _service;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<User> _userManager;

        public ClientController(IServiceManager serviceManager, IAppointmentService appointmentService,
            UserManager<User> userManager)
        {
            _service = serviceManager;
            _appointmentService = appointmentService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            List<ClientIndexViewModel> clients = new List<ClientIndexViewModel>();
            
                var appointments = await _appointmentService.Get();
            if (appointments.Count() != 0 || appointments != null)
            {
                foreach (var appointment in appointments)
                {
                    try
                    {
                        var findService = await _service.GetByIdAsync(appointment.ServiceId);
                        var user = await _userManager.FindByIdAsync(appointment.ClientId);

                        var newClientView = new ClientIndexViewModel
                        {
                            Appointment = appointment,
                            Service = findService,
                            User = user
                        };
                        clients.Add(newClientView);
                    }
                    catch (NullReferenceException)
                    {
                        return BadRequest("Nenhum Agendamento");
                    }
                }
            }

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
