using appointmentApp.Models.Entities;
using appointmentApp.Models.ViewModels;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Models.ViewModels.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace appointmentApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly IAppointmentService _appointmentService;
        private readonly IServiceManager _serviceManager;

        public HomeController(UserManager<UserModel> userManager, IAppointmentService appointmentService, IServiceManager serviceManager)
        {
            _userManager = userManager;
            _appointmentService = appointmentService;
            _serviceManager = serviceManager;
        }

        [Authorize(Roles = "Admin")]
        public ActionResult IndexAdm()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            int pageSize = 10;
            List<ClientIndexViewModel> clients = new List<ClientIndexViewModel>();

            // 1. Pega todos
            var allAppointments = await _appointmentService.Get();

            // 2. FILTRA pelo usuário logado PRIMEIRO (Evita que o loop pegue lixo de outros users)
            var userAppointments = allAppointments.Where(a => a.ClientId == userId).OrderByDescending(a => a.Day).ToList();

            // 3. Agora calcula a paginação baseada apenas nos agendamentos DELE
            int totalItems = userAppointments.Count();
            var pagedAppointments = userAppointments
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var sessionUser = await _userManager.FindByIdAsync(userId);

            foreach (var appointment in pagedAppointments)
            {
                var findService = await _serviceManager.GetByIdAsync(appointment.ServiceId);

                clients.Add(new ClientIndexViewModel
                {
                    Appointment = appointment,
                    Service = findService,
                    User = sessionUser
                });
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(clients);
        }


    }
}
