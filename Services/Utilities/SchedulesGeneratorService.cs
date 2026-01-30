using appointmentApp.Models.Entities;

namespace appointmentApp.Services.Utilities
{
    public class SchedulesGeneratorService
    {
        public static List<TimeSpan> GenerateHours(TimeSpan start, TimeSpan end, DateOnly day, List<AppointmentModel> appointments, List<ServiceModel> services)
        {
            var availableHours = new List<TimeSpan>();

            // Lista de horários já agendados com suas respectivas durações
            var bookedIntervals = appointments
                .Where(a => DateOnly.FromDateTime(a.Day) == day)
                .Select(a =>
                {
                    var service = services.FirstOrDefault(s => s.ServiceId == a.ServiceId);
                    var duration = service != null ? service.Duration : TimeSpan.FromMinutes(30); // Duração padrão se não encontrado
                    return new { Start = a.Day.TimeOfDay, End = a.Day.TimeOfDay + duration };
                })
                .ToList();

            // Percorre o horário de 30 em 30 minutos verificando disponibilidade
            for (TimeSpan h = start; h + TimeSpan.FromMinutes(30) <= end; h += TimeSpan.FromMinutes(30))
            {
                // Verifica se esse horário está dentro de algum intervalo já ocupado
                bool isAvailable = !bookedIntervals.Any(b =>
                    (h >= b.Start && h < b.End) ||  // Está dentro de um intervalo ocupado
                    (h + TimeSpan.FromMinutes(30) > b.Start && h + TimeSpan.FromMinutes(30) <= b.End) // Próximo bloco também está ocupado
                );

                if (isAvailable)
                {
                    availableHours.Add(h);
                }
            }

            return availableHours;
        }

    }
}
