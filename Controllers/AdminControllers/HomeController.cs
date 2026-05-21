using appointmentApp.Models.Entities;
using appointmentApp.Models.ViewModels;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Models.ViewModels.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AppointmentAppMvc.Controllers.AdminControllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly IAppointmentService _appointmentService;
        private readonly IServiceManager _serviceManager;
        private static DateTime _lastKeepAlive = DateTime.MinValue;

        public HomeController(UserManager<UserModel> userManager, IAppointmentService appointmentService, IServiceManager serviceManager)
        {
            _userManager = userManager;
            _appointmentService = appointmentService;
            _serviceManager = serviceManager;

            //Task para impedir do banco desligar
            if ((DateTime.Now - _lastKeepAlive).TotalHours > 24)
            {
                _lastKeepAlive = DateTime.Now;

                Task.Run(async () =>
                {
                    try
                    {

                        await _userManager.Users.Take(1).ToListAsync();
                    }
                    catch
                    {
                        _lastKeepAlive = DateTime.MinValue;
                    }
                });
            }
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

            // 3. calcula a paginação baseada apenas nos agendamentos DELE
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
        [HttpGet]
        public async Task<IActionResult> IndexWorker(int page = 1)
        {
            int pageSize = 10;
            List<ClientIndexViewModel> clients = new List<ClientIndexViewModel>();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var allAppointments = await _appointmentService.Get();

            allAppointments = allAppointments.Where(x => x.WorkerId == userId);

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
                var findService = await _serviceManager.GetByIdAsync(appointment.ServiceId);
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
        [AllowAnonymous]
        public ActionResult OfflineDbError()
        {
            return View();
        }


    }
}
