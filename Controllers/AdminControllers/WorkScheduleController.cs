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
                        // Criamos a escala do trabalhador para a semana baseada na global
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

                        // Recarrega atualizado do banco para evitar duplicidade em F5/reloads rápidos
                        schedules = await _scheduleService.GetAsync(startDate, endDate);
                        workerSchedules = schedules.Where(ws => ws.WorkerId == workerId.Value).ToList();
                    }
                }

                mappedSchedules = workerSchedules
                    .OrderBy(ws => ws.Day)
                    .Select(ws => new DayScheduleViewModel
                    {
                        Id = ws.Id,
                        WokerId = ws.WorkerId, // Mantido o padrão da sua propriedade do ViewModel
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

                if (!globalSchedules.Any())
                {
                    for (int i = 0; i < 7; i++)
                    {
                        DateTime targetDay = startDate.AddDays(i);

                        var newGlobalDay = new WorkScheduleModel(
                            targetDay,
                            false,
                            new TimeSpan(8, 0, 0),
                            new TimeSpan(18, 0, 0),
                            null
                        );

                        await _scheduleService.Create(newGlobalDay);
                    }

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
        public async Task<IActionResult> WorkSchedule(List<DayScheduleViewModel> models, Guid? workerId, int offsetWeeks = 0)
        {
            if (models == null || !models.Any())
            {
                TempData["ErrorMessage"] = "Nenhum dado de escala foi recebido.";
                return RedirectToAction(nameof(WorkSchedule), new { workerId, offsetWeeks });
            }

            foreach (var model in models)
            {
                if (model.Start > model.End)
                {
                    ModelState.AddModelError("", $"Horário inválido para o dia {model.Day.ToShortDateString()}: O início não pode ser após o fim.");

                    ViewBag.Workers = await _userManager.GetUsersInRoleAsync("Worker");
                    ViewBag.CurrentWorkerId = workerId;
                    ViewBag.OffsetWeeks = offsetWeeks;
                    ViewBag.StartDate = DateTime.Today.AddDays(offsetWeeks * 7);
                    return View(models);
                }
            }

            DateTime startDate = DateTime.Today.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);
            var allSchedulesFromWeek = await _scheduleService.GetAsync(startDate, endDate);

            foreach (var model in models)
            {
                WorkScheduleModel existingSchedule = workerId.HasValue
                    ? allSchedulesFromWeek.FirstOrDefault(d => d.Day.Date == model.Day.Date && d.WorkerId == workerId.Value)
                    : allSchedulesFromWeek.FirstOrDefault(d => d.Day.Date == model.Day.Date && d.WorkerId == null);

                if (existingSchedule != null)
                {
                    existingSchedule.SetStart(model.Start);
                    existingSchedule.SetEnd(model.End);
                    existingSchedule.SetIsSelected(model.IsSelected);

                    await _scheduleService.UpdateAsync(existingSchedule);
                }
                else
                {
                    ModelState.AddModelError("", $"A escala para o dia {model.Day.ToShortDateString()} não foi localizada no banco.");
                    ViewBag.Workers = await _userManager.GetUsersInRoleAsync("Worker");
                    return View(models);
                }
            }

            TempData["SuccessMessage"] = "Escala de trabalho atualizada com sucesso!";

            return RedirectToAction(nameof(WorkSchedule), new { workerId = workerId, offsetWeeks = offsetWeeks });
        }
    }
}