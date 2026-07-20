using appointmentApp.Models.Entities;
using appointmentApp.Services.WorkScheduleService;
using AppointmentAppMvc.Models.ViewModels.WorkSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAppMvc.Controllers.AdminControllers
{
    [Authorize(Roles = "Admin")]
    public class WorkScheduleController : Controller
    {
        private readonly IWorkScheduleService _scheduleService;
        private readonly UserManager<UserModel> _userManager;

        public WorkScheduleController(IWorkScheduleService scheduleService, UserManager<UserModel> userManager)
        {
            _scheduleService = scheduleService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> WorkSchedule(Guid? workerId, int offsetWeeks = 0)
        {
            DateTime startDate = DateTime.Today.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);

            var workers = await _userManager.GetUsersInRoleAsync("Worker");
            ViewBag.Workers = workers;
            ViewBag.CurrentWorkerId = workerId;

            // 1. Busca tudo o que já existe no banco para o período
            var schedules = await _scheduleService.GetAsync(startDate, endDate);
            List<DayScheduleViewModel> mappedSchedules;

            if (workerId.HasValue)
            {
                var selectedWorker = workers.FirstOrDefault(w => w.Id == workerId.Value.ToString());
                ViewBag.CurrentWorkerName = selectedWorker?.UserName ?? "Profissional";

                var workerSchedules = schedules.Where(ws => ws.WorkerId == workerId.Value).ToList();

                if (!workerSchedules.Any())
                {
                    var globalSchedules = schedules.Where(ws => ws.WorkerId == null).ToList();

                    if (globalSchedules.Any())
                    {
                        foreach (var globalDay in globalSchedules)
                        {
                            var newScheduleForWorker = new WorkScheduleModel(
                                globalDay.Day,
                                globalDay.IsSelected,
                                globalDay.Start,
                                globalDay.End,
                                workerId.Value
                            );
                            await _scheduleService.Create(newScheduleForWorker);
                        }

                        schedules = await _scheduleService.GetAsync(startDate, endDate);
                        workerSchedules = schedules.Where(ws => ws.WorkerId == workerId.Value).ToList();
                    }
                }

                mappedSchedules = workerSchedules
                    .OrderBy(ws => ws.Day)
                    .Select(ws => new DayScheduleViewModel
                    {
                        Id = ws.Id,
                        WokerId = ws.WorkerId,
                        Day = ws.Day,
                        Start = ws.Start,
                        End = ws.End,
                        IsSelected = ws.IsSelected
                    })
                    .ToList();
            }
            else
            {
                ViewBag.CurrentWorkerName = "Escala Padrão da Empresa";

                var globalSchedules = schedules.Where(ws => ws.WorkerId == null).ToList();

                // CORREÇÃO AQUI: Se a escala geral da empresa para essa semana não existe no banco, nós criamos!
                if (!globalSchedules.Any())
                {
                    for (int i = 0; i < 7; i++)
                    {
                        DateTime targetDay = startDate.AddDays(i);

                        // Cria uma linha padrão desmarcada (ou ajuste os horários padrão da sua barbearia aqui)
                        var newGlobalDay = new WorkScheduleModel(
                            targetDay,
                            false, // Inicialmente desmarcado (folga) até o admin ativar
                            new TimeSpan(8, 0, 0),  // 08:00
                            new TimeSpan(18, 0, 0), // 18:00
                            null // WorkerId é null porque é a escala padrão da empresa
                        );

                        await _scheduleService.Create(newGlobalDay);
                    }

                    // Recarrega do banco após criar os 7 dias padrões
                    schedules = await _scheduleService.GetAsync(startDate, endDate);
                    globalSchedules = schedules.Where(ws => ws.WorkerId == null).ToList();
                }

                mappedSchedules = globalSchedules
                    .OrderBy(ws => ws.Day)
                    .Select(ws => new DayScheduleViewModel
                    {
                        Id = ws.Id,
                        WokerId = null,
                        Day = ws.Day,
                        Start = ws.Start,
                        End = ws.End,
                        IsSelected = ws.IsSelected
                    })
                    .ToList();
            }

            ViewBag.OffsetWeeks = offsetWeeks;
            ViewBag.StartDate = startDate;

            return View(mappedSchedules);
        }
        [HttpPost]
        public async Task<IActionResult> WorkSchedule(
            [Bind("Id,Day,Start,End,IsSelected")] List<DayScheduleViewModel> models,
            Guid? WorkerId,
            int offsetWeeks = 0)
        {
            if (models == null || models.Count == 0)
            {
                return BadRequest("Model inválido ou vazio.");
            }

            foreach (var model in models)
            {
                if (model.Start > model.End)
                {
                    return BadRequest($"Horário inválido para o dia {model.Day.ToShortDateString()}: O início não pode ser maior que o fim.");
                }
            }

            DateTime startDate = DateTime.Today.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);

            var allSchedulesFromWeek = await _scheduleService.GetAsync(startDate, endDate);

            foreach (var model in models)
            {
                WorkScheduleModel newschedule = null;

                if (WorkerId.HasValue)
                {
                    newschedule = allSchedulesFromWeek
                        .FirstOrDefault(d => d.Day == model.Day && d.WorkerId == WorkerId.Value);
                }
                else
                {
                    newschedule = allSchedulesFromWeek
                        .FirstOrDefault(d => d.Day == model.Day && d.WorkerId == null);
                }

                if (newschedule != null)
                {
                    newschedule.SetStart(model.Start);
                    newschedule.SetEnd(model.End);
                    newschedule.SetIsSelected(model.IsSelected);

                    await _scheduleService.UpdateAsync(newschedule);
                }
                else
                {
                    return NotFound($"Nenhum horário encontrado para o dia {model.Day.ToShortDateString()} correspondente ao profissional selecionado.");
                }
            }

            return RedirectToAction("IndexAdm", "Home");
        }
    }
}
