using appointmentApp.Models.ViewModels;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using Microsoft.AspNetCore.Mvc;

namespace appointmentApp.Controllers
{
    public class ServiceController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly IAppointmentService _appointments;
        public ServiceController(IServiceManager serviceManager, IAppointmentService appointmentService)
        {
            _serviceManager = serviceManager;
            _appointments = appointmentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> Index(string? txt)
        {
            var services = await _serviceManager.GetAsync(txt);

            return View("Index", services);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(CreateServiceViewModel model)
        {

            if(model.Duration > new TimeSpan(6, 0, 0))
            {
                TempData["durationExceed"] = "Não é possível criar um serviço tão longo";
            }
            try
            {
                var service = new Service(model.ServiceName, model.Price, model.Duration, model.Category);

                await _serviceManager.CreateAsync(service);

                return RedirectToAction("Index");

            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = "This field is required " ;
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, UpdateServiceViewModel serviceView)
        {
            var appointmentExists = await _appointments.GetByServiceId(id);
            if (appointmentExists != null)
            {
                TempData["AppointmentExists"] = "Existe um agendamento para este serviço, não pode Modificar";
                return RedirectToAction("index");

            }
            else
            {
                var dbService = await _serviceManager.GetByIdAsync(id);

                if (!String.IsNullOrEmpty(serviceView.ServiceName))
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
                if (!String.IsNullOrEmpty(serviceView.Category))
                {
                    dbService.SetCategory(serviceView.Category);
                }
                try
                {
                    await _serviceManager.UpdateAsync(dbService);
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error to update service {ex}{dbService}");
                }


                return RedirectToAction("Index");
            }
            

        }
        [HttpGet]
        public async Task<IActionResult> Delete()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var appointmentExists = await _appointments.GetByServiceId(id);
            if (appointmentExists != null)
            {
                TempData["AppointmentExists"] = "Existe um agendamento para este serviço, não pode deletar";
                return RedirectToAction("index");
                
            }

            var dbService = await _serviceManager.GetByIdAsync(id);
            
            
            try
            {
                await _serviceManager.DeleteAsync(dbService);

                return RedirectToAction("Index");
                
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao Deletar o serviço {ex}{dbService}");
            }
        }

    }
}
