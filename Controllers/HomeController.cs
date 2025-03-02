using appointmentApp.Models.Entities;
using appointmentApp.Models.ViewModels;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace appointmentApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IAppointmentService _appointmentService;
        private readonly IServiceManager _serviceManager;

        public HomeController(UserManager<User> userManager, IAppointmentService appointmentService, IServiceManager serviceManager)
        {
            _userManager = userManager;
            _appointmentService = appointmentService;
            _serviceManager = serviceManager;
        }

        public ActionResult IndexAdm()
        {
            return View();
        }
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return BadRequest(ModelState);
            }

            var appointment = await _appointmentService.GetByUserId(userId);
            
            if (appointment == null)
            {
                return View();
            }
            else
            {  
                var userInfo = await _userManager.FindByIdAsync(userId);
                if (userInfo == null)
                {
                    return BadRequest(ModelState);
                }
                if(appointment.ServiceId == null)
                {
                    var services = await _serviceManager.GetAsync("");
                    var service = services.FirstOrDefault();
                }
                var serviceSelected = await _serviceManager.GetByIdAsync(appointment.ServiceId);

                var newModel = new ClientIndexViewModel
                {
                    Appointment = appointment,
                    User = userInfo,
                    Service = serviceSelected
                };
                return View(newModel);
            }
        }

    }
}
