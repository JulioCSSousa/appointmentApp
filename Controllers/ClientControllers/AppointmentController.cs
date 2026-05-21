using appointmentApp.Models.Entities;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using appointmentApp.Services.Utilities;
using appointmentApp.Services.WorkScheduleService;
using AppointmentAppMvc.Hubs;
using AppointmentAppMvc.Models.ViewModels;
using AppointmentAppMvc.Models.ViewModels.Account;
using AppointmentAppMvc.Models.ViewModels.Appointment;
using AppointmentAppMvc.Services.Utilities.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AppointmentAppMvc.Controllers.ClientControllers
{
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appService;
        private readonly IServiceManager _serviceManager;
        private readonly IWorkScheduleService _workSchedule;
        private readonly UserManager<UserModel> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AppointmentController(IAppointmentService appservice,
            IServiceManager serviceManager,
            IWorkScheduleService workschedule,
            IHubContext<NotificationHub> hubContext,
            UserManager<UserModel> userManager)

        {
            _appService = appservice;
            _serviceManager = serviceManager;
            _workSchedule = workschedule;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        private async Task<AppointmentViewModel> LoadAppointmentViewModelAsync(int offsetWeeks = 0)
        {
            // 0. Define o fuso horário de Brasília 
            TimeZoneInfo brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            DateTime todayInBrazil = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brazilTimeZone).Date;
            DateTime nowInBrazil = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brazilTimeZone);

            // Ajusta as datas de início e fim com base no dia correto do Brasil
            DateTime startDate = todayInBrazil.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);

            // 1. Buscas
            var allServices = (await _serviceManager.GetAsync("")).ToList();
            var workSchedules = await _workSchedule.GetAsync(startDate, endDate);
            var appointments = await _appService.Get();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenList = new List<bool>();

            // 2. Filtrar os schedules que são de hoje em diante (baseado no Brasil)
            var schedulesConfigurados = workSchedules
                .Where(ws => ws.Day.Date >= todayInBrazil)
                .OrderBy(ws => ws.Day.Date)
                .ToList();

            foreach (var workSchedule in schedulesConfigurados)
            {
                // Gerar os horários padrões do dia
                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    appointments.ToList(),
                    allServices
                );

                // Aplica o filtro de margem APENAS para o dia de HOJE
                if (workSchedule.Day.Date == todayInBrazil)
                {
                    // Usa o horário atual do Brasil + 20 minutos de margem
                    var margem = nowInBrazil.TimeOfDay.Add(TimeSpan.FromMinutes(20));
                    hoursInDay = hoursInDay.Where(h => h > margem).ToList();
                }

                // Adiciona ao dicionário usando a data limpa
                appointmentDateAndHourList[workSchedule.Day.Date] = hoursInDay;

                // Sincroniza se o dia está aberto (IsSelected == true significa Aberto)
                isDayOpenList.Add(workSchedule.IsSelected);
            }

            var usersWithRoles = await _userManager.GetUsersInRoleAsync("Colaborador");
            var workersModel = new List<UserWithRolesViewModel>();

            foreach (var item in usersWithRoles)
            {
                workersModel.Add(new UserWithRolesViewModel
                {
                    Id = item.Id,
                    Name = item.Name 
                });
            }

            return new AppointmentViewModel
            {
                Services = allServices,
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = isDayOpenList,
                Workers = workersModel 
            };
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            var appointmentsViewModel = await LoadAppointmentViewModelAsync();
            return View(appointmentsViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AppointmentViewModel model)
        {
            if (string.IsNullOrEmpty(model.ClientId))
                return BadRequest("Client ID não encontrado");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID não encontrado");

            if (model.SelectedServiceId == null)
                return BadRequest("Nenhum serviço foi selecionado.");

            if (model.SelectedDayAndHour == default)
                return BadRequest("Nenhuma data e horário foram selecionados.");

            try
            {
                var selectedService = await _serviceManager.GetByIdAsync(model.SelectedServiceId);
                if (selectedService == null)
                {
                    return BadRequest("Serviço não encontrado no banco de dados.");
                }

                var newAppointment = new AppointmentModel(Guid.NewGuid(), userId, model.SelectedServiceId, model.WorkerId, model.SelectedDayAndHour, "Waiting");

                await _appService.Create(newAppointment);
                TempData["SuccessMessage"] = "Agendamento realizado com sucesso!";
                await _hubContext.Clients.All.SendAsync("ReceiveNewAppointment", "Novo agendamento realizado!");
                ViewBag.NewAppointment = "New";
                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", $"Erro ao criar agendamento: {ex.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocorreu um erro inesperado ao tentar criar o agendamento.");
            }

            // Se falhar, recarrega a view com os dados necessários
            var appointmentsViewModel = await LoadAppointmentViewModelAsync();
            return View(appointmentsViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Boa prática de segurança
        public async Task<ActionResult> UpdateStatus(Guid id, Status newStatus)
        {
            var dbappointment = await _appService.GetById(id);

            if (dbappointment == null)
                return NotFound("Este agendamento não existe.");

            // Atualiza o status no objeto da entidade
            dbappointment.SetStatus(newStatus);

            // PERSISTÊNCIA: Você precisa chamar o serviço para salvar no banco!
            await _appService.Update(dbappointment);

            TempData["SuccessMessage"] = "Status atualizado com sucesso!";
            string refererUrl = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl); // Redireciona exatamente para a mesma URL atual
            }
            // Redireciona para onde o usuário estava (ex: IndexAdm da Home)
            return View();
        }
        // GET: AppointmentController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        public async Task<ActionResult> CancelAppointment(Guid id, Status newStatus)
        {
            var dbappointment = await _appService.GetById(id);

            if (dbappointment == null)
                return NotFound("Este agendamento não existe.");

            // Atualiza o status no objeto da entidade
            dbappointment.SetStatus(Status.Canceled);

            // PERSISTÊNCIA: Você precisa chamar o serviço para salvar no banco!
            await _appService.Update(dbappointment);

            TempData["SuccessMessage"] = "Status atualizado com sucesso!";

            // Redireciona para onde o usuário estava (ex: IndexAdm da Home)
            return Ok();
        }

        public async Task<IActionResult> Delete(Guid id)
        {

            var appointment = await _appService.GetById(id);
            if (appointment == null)
            {
                return BadRequest("Id not Existis");
            }
            await _appService.Delete(appointment);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (userRoles.Contains("Admin"))
                return RedirectToAction("IndexAdm", "Home");
            else
                return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestAppointmentId()
        {
            var lastApp = (await _appService.Get()).OrderByDescending(x => x.Day).FirstOrDefault();
            return Json(new { id = lastApp?.Id, client = lastApp?.ClientId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableHours(string workerId, int offsetWeeks = 0)
        {
            if (string.IsNullOrEmpty(workerId))
            {
                return BadRequest("Colaborador não especificado.");
            }

            DateTime startDate = DateTime.Today.AddDays(offsetWeeks * 7);
            DateTime endDate = startDate.AddDays(6);

            // 1. Buscas obrigatórias para gerar os horários
            var allServices = await _serviceManager.GetAsync("");

            // FILTRO 1: Ajuste aqui se o seu repositório aceitar o ID do worker por parâmetro,
            // caso contrário, filtramos com .Where() logo abaixo.
            var workSchedules = await _workSchedule.GetAsync(startDate, endDate);

            // FILTRO 2: Buscando os agendamentos já existentes
            var appointments = await _appService.Get();

            // Aplicando os filtros baseados no barbeiro selecionado
            var schedulesDoTrabalhador = workSchedules
                .Where(ws => ws.WorkerId.ToString() == workerId) // Substitua 'UserId' ou 'WorkerId' pela propriedade real do seu WorkSchedule
                .Where(ws => ws.Day >= DateTime.Today)
                .OrderBy(ws => ws.Day.Date)
                .ToList();

            var appointmentsDoTrabalhador = appointments
                .Where(ap => ap.WorkerId == workerId) // Substitua 'WorkerId' ou 'UserId' pela propriedade real do seu Appointment
                .ToList();

            var appointmentDateAndHourList = new Dictionary<DateTime, List<TimeSpan>>();
            var isDayOpenList = new List<bool>();

            // 2. Processamento dos horários específicos do colaborador
            foreach (var workSchedule in schedulesDoTrabalhador)
            {
                var hoursInDay = SchedulesGeneratorService.GenerateHours(
                    workSchedule.Start,
                    workSchedule.End,
                    DateOnly.FromDateTime(workSchedule.Day),
                    appointmentsDoTrabalhador, // Passa apenas os agendamentos dele
                    allServices.ToList()
                );

                // Filtro para o dia de hoje (evita mostrar horários passados)
                if (workSchedule.Day.Date == DateTime.Today)
                {
                    var margem = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(20));
                    hoursInDay = hoursInDay.Where(h => h > margem).ToList();
                }

                appointmentDateAndHourList[workSchedule.Day.Date] = hoursInDay;
                isDayOpenList.Add(workSchedule.IsSelected);
            }

            // 3. Monta o ViewModel simplificado apenas com o que a Partial View da tabela precisa
            var viewModel = new AppointmentViewModel
            {
                HoursToSchedule = appointmentDateAndHourList,
                IsDayClosed = isDayOpenList
            };

            // Retorna apenas o pedaço de HTML correspondente à tabela
            return PartialView("_HoursTablePartial", viewModel);
        }
    }
}
