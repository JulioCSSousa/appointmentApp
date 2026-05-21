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

            var workers = await _userManager.GetUsersInRoleAsync("Colaborador");
            ViewBag.Workers = workers;
            ViewBag.CurrentWorkerId = workerId;

            var schedules = await _scheduleService.GetAsync(startDate, endDate);
            List<DayScheduleViewModel> mappedSchedules;

            if (workerId.HasValue)
            {
                var selectedWorker = workers.FirstOrDefault(w => w.Id == workerId.Value.ToString());
                ViewBag.CurrentWorkerName = selectedWorker?.UserName ?? "Profissional";

                // 1. Filtra as escalas que pertencem ao colaborador selecionado
                var workerSchedules = schedules.Where(ws => ws.WorkerId == workerId.Value).ToList();

                // 2. SE O BANCO ESTIVER VAZIO PARA ELE NESSA SEMANA: Vamos preencher agora!
                if (!workerSchedules.Any())
                {
                    // Busca a escala padrão (null) dessa mesma semana para usar de cópia
                    var globalSchedules = schedules.Where(ws => ws.WorkerId == null).ToList();

                    if (globalSchedules.Any())
                    {
                        foreach (var globalDay in globalSchedules)
                        {
                            // Clona os dias, horários e status da empresa, mas atrela ao ID do colaborador
                            var newScheduleForWorker = new WorkScheduleModel(
                                globalDay.Day,
                                globalDay.IsSelected,
                                globalDay.Start,
                                globalDay.End,
                                workerId.Value
                            );

                            // Salva direto no contexto (insira a chamada do seu repositório/contexto aqui)
                            await _scheduleService.Create(newScheduleForWorker);
                        }

                        // Recarrega os dados atualizados do serviço para o fluxo seguir normalmente
                        schedules = await _scheduleService.GetAsync(startDate, endDate);
                        workerSchedules = schedules.Where(ws => ws.WorkerId == workerId.Value).ToList();
                    }
                }

                // 3. Mapeia para a View (Agora com a garantia de que os dados existem no banco)
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
                // Cenário B: Escala Geral da Empresa (WorkerId == null)
                ViewBag.CurrentWorkerName = "Escala Padrão da Empresa";

                mappedSchedules = schedules
                    .Where(ws => ws.WorkerId == null)
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

            // 1. Validação inicial de consistência de horários para evitar chamadas desnecessárias ao banco
            foreach (var model in models)
            {
                if (model.Start > model.End)
                {
                    return BadRequest($"Horário inválido para o dia {model.Day.ToShortDateString()}: O início não pode ser maior que o fim.");
                }
            }

            DateTime startDate = DateTime.Today.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);

            // 2. Busca todas as escalas daquela semana no banco de dados de uma vez só (Performance!)
            var allSchedulesFromWeek = await _scheduleService.GetAsync(startDate, endDate);

            foreach (var model in models)
            {
                WorkScheduleModel newschedule = null;

                // 3. IDENTIFICAÇÃO: Filtra o registro correto baseado no cenário
                if (WorkerId.HasValue)
                {
                    // Cenário A: Atualizando um Colaborador específico
                    newschedule = allSchedulesFromWeek
                        .FirstOrDefault(d => d.Day == model.Day && d.WorkerId == WorkerId.Value);
                }
                else
                {
                    // Cenário B: Atualizando a Barbearia (Escala Padrão)
                    newschedule = allSchedulesFromWeek
                        .FirstOrDefault(d => d.Day == model.Day && d.WorkerId == null);
                }

                // 4. Se encontrou o registro correto, atualiza os dados
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

            // Como você está usando paginação e filtros, pode ser interessante redirecionar mantendo o estado na URL,
            // ou voltar para a Home como você já estava fazendo:
            return RedirectToAction("IndexAdm", "Home");
        }
    }
}
