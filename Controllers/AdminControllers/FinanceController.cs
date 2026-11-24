using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using AppointmentAppMvc.Models.ViewModels.Finance;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AppointmentAppMvc.Services.Stores;
using appointmentApp.Constants;

namespace AppointmentAppMvc.Controllers.AdminControllers
{
    public class FinanceController : Controller
    {
        private readonly IServiceManager _service;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<UserModel> _userManager;
        private readonly IStoreContext _storeContext;
        public FinanceController(IServiceManager service, IAppointmentService appointmentService, UserManager<UserModel> userManager, IStoreContext storeContext)
        {
            _service = service;
            _appointmentService = appointmentService;
            _userManager = userManager;
            _storeContext = storeContext;
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> Index()
        {
            double profit = 0;
            var model = new List<FinanceViewModel>();
            var clientsWithAppointments = await _appointmentService.Get();
            if (clientsWithAppointments == null || clientsWithAppointments.Count() == 0)
            {
                return View(model);
            }
            var filteredClients = clientsWithAppointments.Where(c => c.Status == "Finished");
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null)
            {
                return NotFound("Loja não encontrada.");
            }

            foreach (var item in filteredClients)
            {
                var getClientById = string.IsNullOrWhiteSpace(item.ClientId)
                    ? null
                    : await _userManager.Users
                        .SingleOrDefaultAsync(user => user.Id == item.ClientId && user.StoreId == store.Id);
                if (getClientById == null)
                {
                    return View();
                }

                var price = item.ServicePrice;
                var serviceName = item.ServiceName;
                profit += price;

                model.Add(new FinanceViewModel
                {
                    ClientName = getClientById.Name,
                    ServiceName = serviceName,
                    Date = item.Day,
                    Profits = profit,
                    ServicePrice = price
                });
            }

            return View(model);
        }
    }
}
