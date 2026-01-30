using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using AppointmentAppMvc.Models.ViewModels.Finance;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace appointmentApp.Controllers
{
    public class FinanceController : Controller
    {
        private readonly IServiceManager _service;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<UserModel> _userManager;
        public FinanceController(IServiceManager service, IAppointmentService appointmentService, UserManager<UserModel> userManager)
        {
            _service = service;
            _appointmentService = appointmentService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            double profit = 0;
            var model = new List<FinanceViewModel>();
            var clientsWithAppointments = await _appointmentService.Get();
            if (clientsWithAppointments == null || clientsWithAppointments.Count() == 0)
            {
                return BadRequest("Has no Appointments");
            }
            var filteredClients = clientsWithAppointments.Where(c => c.Status == "Finished");

            foreach (var item in filteredClients)
            {
                var getClientById = await _userManager.FindByIdAsync(item.ClientId);
                if (getClientById == null)
                {
                    return NotFound("Client was not found");
                }
                var getServiceById = await _service.GetByIdAsync(item.ServiceId);
                if (getServiceById == null)
                {
                    return NotFound("Service was not found");
                }
                var price = getServiceById.Price;

                profit += price;

                model.Add(new FinanceViewModel
                {
                    ClientName = getClientById.Name,
                    ServiceName = getServiceById.ServiceName,
                    Date = item.Day,
                    Profits = profit,
                    ServicePrice = getServiceById.Price
                });
            }

            return View(model);
        }
    }
}
