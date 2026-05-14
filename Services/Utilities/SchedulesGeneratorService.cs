using appointmentApp.Models.Entities;

namespace appointmentApp.Services.Utilities
{
    public class SchedulesGeneratorService
    {
        public static List<TimeSpan> GenerateHours(TimeSpan start, TimeSpan end, DateOnly day, List<AppointmentModel> appointments, List<ServiceModel> services)
        {
            var availableHours = new List<TimeSpan>();

            // REMOVIDO: start = DateTime.Now.TimeOfDay... (Isso quebrava os dias futuros)

            // 1. Identifica os intervalos ocupados para este dia específico
            var bookedIntervals = appointments
                .Where(a => DateOnly.FromDateTime(a.Day) == day &&
                            a.Status != "Canceled" &&
                            a.Status != "Finished")
                .Select(a =>
                {
                    var service = services.FirstOrDefault(s => s.ServiceId == a.ServiceId);
                    var duration = service != null ? service.Duration : TimeSpan.FromMinutes(30);
                    return new { Start = a.Day.TimeOfDay, End = a.Day.TimeOfDay + duration };
                })
                .ToList();

            // 2. Loop de geração de slots (ex: 30 em 30 min)
            TimeSpan interval = TimeSpan.FromMinutes(30);

            for (TimeSpan h = start; h + interval <= end; h += interval)
            {
                // Verifica se o slot h até h+interval sobrepõe algum agendamento existente
                bool isBooked = bookedIntervals.Any(b => h < b.End && b.Start < (h + interval));

                if (!isBooked)
                {
                    availableHours.Add(h);
                }
            }

            return availableHours;
        }

    }
}
