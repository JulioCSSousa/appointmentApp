using appointmentApp.Services.WorkScheduleService;
using AppointmentAppMvc.Models.ViewModels.WorkSchedule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace appointmentApp.Controllers
{
    public class WorkScheduleController : Controller
    {
        private readonly IWorkScheduleService _scheduleService;

        public WorkScheduleController(IWorkScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet]
        public async Task<IActionResult> SaveWorkSchedule()
        {
            // Obter dados do banco e mapear para a ViewModel
            var schedules = await _scheduleService.GetAsync();
               var newSchedules = schedules.Select(ws => new DayScheduleViewModel
            {
                Id = ws.Id,
                Day = ws.Day,
                Start = ws.Start,
                End = ws.End,
                IsSelected = ws.IsSelected
            });

            return View(newSchedules);
        }

        [HttpPost]
        public async Task<IActionResult> SaveWorkSchedule([Bind("Day,Start,End,IsSelected")] List<DayScheduleViewModel> models)
        {
            if (models == null || models.Count == 0)
            {
                return BadRequest("Model inválido ou vazio.");
            }

            foreach (var model in models)
            {

                if(model.Start > model.End)
                {
                    return BadRequest("Agendamento inválido!");
                }
                // Busca o horário do dia específico no banco
                var schedule = await _scheduleService.GetAsync();
                var newschedule = schedule.FirstOrDefault(d => d.Day == model.Day);
                if (newschedule != null)
                {
                    newschedule.SetStart(model.Start);
                    newschedule.SetEnd(model.End);
                    newschedule.SetIsSelected(model.IsSelected);

                    await _scheduleService.UpdateAsync(newschedule); ;
                }
                else
                {
                    return NotFound($"Nenhum horário encontrado para o dia {model.Day}");
                }
            }

            return RedirectToAction("IndexAdm", "Home");
        }
    }
}
