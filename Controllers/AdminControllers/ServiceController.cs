using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using AppointmentAppMvc.Models.ViewModels.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentAppMvc.Services.Stores;

namespace AppointmentAppMvc.Controllers.AdminControllers
{
    [Authorize(Roles = "Admin")]
    public class ServiceController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly IAppointmentService _appointments;
        private readonly IStoreContext _storeContext;

        public ServiceController(IServiceManager serviceManager, IAppointmentService appointmentService, IStoreContext storeContext)
        {
            _serviceManager = serviceManager;
            _appointments = appointmentService;
            _storeContext = storeContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? txt)
        {
            var services = await _serviceManager.GetAsync(txt);
            return View("Index", services);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateServiceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Temporário para ver no console qual campo deu erro:
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}");
                Console.WriteLine(string.Join(" | ", errors));

                return View(model);
            }

            if (model.Duration > new TimeSpan(12, 0, 0))
            {
                ModelState.AddModelError("Duration", "Service duration cannot exceed 12 hours (maximum daily shift).");
                return View(model);
            }

            try
            {
                var service = new ServiceModel(model.ServiceName, model.Price, model.Duration, model.Category);
                var store = await _storeContext.GetCurrentStoreAsync();
                if (store == null)
                {
                    ModelState.AddModelError(string.Empty, "Nenhuma loja ativa foi configurada.");
                    return View(model);
                }
                service.SetStore(store.Id);
                await _serviceManager.CreateAsync(service);

                TempData["SuccessMessage"] = "Service created successfully.";
                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the service.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dbService = await _serviceManager.GetByIdAsync(id);
            if (dbService == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("Index");
            }

            var model = new UpdateServiceViewModel
            {
                ServiceId = dbService.ServiceId,
                ServiceName = dbService.ServiceName,
                Category = dbService.Category,
                Duration = dbService.Duration,
                Price = dbService.Price
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateServiceViewModel serviceView)
        {
            var appointmentExists = await _appointments.GetByServiceId(id);
            if (appointmentExists != null && appointmentExists.Status != "Canceled" && appointmentExists.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Cannot edit this service because there is an active appointment linked to it.";
                return RedirectToAction("Index");
            }

            var dbService = await _serviceManager.GetByIdAsync(id);
            if (dbService == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(serviceView.ServiceName))
            {
                dbService.SetServiceName(serviceView.ServiceName);
            }
            if (serviceView.Price != null)
            {
                dbService.SetPrice((double)serviceView.Price);
            }
            if (serviceView.Duration != null)
            {
                dbService.SetDuration((TimeSpan)serviceView.Duration);
            }
            if (!string.IsNullOrEmpty(serviceView.Category))
            {
                dbService.SetCategory(serviceView.Category);
            }

            try
            {
                await _serviceManager.UpdateAsync(dbService);
                TempData["SuccessMessage"] = "Service updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while trying to update the service. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var appointmentExists = await _appointments.GetByServiceId(id);
            if (appointmentExists != null && appointmentExists.Status != "Canceled" && appointmentExists.Status != "Finished")
            {
                TempData["DeleteError"] = "Cannot delete this service because there is an active appointment linked to it.";
                return RedirectToAction("Index");
            }

            var dbService = await _serviceManager.GetByIdAsync(id);
            if (dbService == null)
            {
                TempData["DeleteError"] = "Service not found.";
                return RedirectToAction("Index");
            }

            try
            {
                await _serviceManager.DeleteAsync(dbService);
                TempData["SuccessMessage"] = "Service deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while trying to delete the service. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}