using appointmentApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace appointmentApp.ApplicationSeed
{
    public class SeedWorkSchedule
    {
        private const int SCHEDULE_WINDOW_DAYS = 14;

        public static async Task SeedWorkSchedules(AppDbContext context, ILogger<SeedWorkSchedule> logger)
        {
            try
            {

                TimeZoneInfo brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brazilTimeZone).Date;
                DateTime limitDate = today.AddDays(SCHEDULE_WINDOW_DAYS);

                var workerRoleId = await context.Roles
                    .Where(r => r.Name == "Worker")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                // 2. Busca os IDs de todos os usuários que estão vinculados a essa Role
                var activeWorkerIds = new List<string>();

                if (!string.IsNullOrEmpty(workerRoleId))
                {
                    activeWorkerIds = await context.UserRoles
                        .Where(ur => ur.RoleId == workerRoleId)
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                }

                bool hasChanges = false;

                for (DateTime currentDay = today; currentDay <= limitDate; currentDay = currentDay.AddDays(1))
                {
                    if (!activeWorkerIds.Any())
                    {
                        bool existsGlobal = await context.WorkSchedules
                            .AnyAsync(ws => ws.Day.Date == currentDay.Date && ws.WorkerId == null);

                        if (!existsGlobal)
                        {
                            var template = await context.WorkSchedules
                                .Where(ws => ws.Day.DayOfWeek == currentDay.DayOfWeek && ws.WorkerId == null)
                                .OrderByDescending(ws => ws.Day)
                                .FirstOrDefaultAsync();

                            var (isSelected, start, end) = GetTemplateHours(template);

                            var newGlobalSchedule = new WorkScheduleModel(currentDay, isSelected, start, end, null);
                            context.WorkSchedules.Add(newGlobalSchedule);
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        foreach (var workerId in activeWorkerIds)
                        {
                            var targetId = Guid.Parse(workerId);

                            bool existsForWorker = await context.WorkSchedules
                                .AnyAsync(ws => ws.Day.Date == currentDay.Date && ws.WorkerId == targetId);

                            if (!existsForWorker)
                            {
                                var template = await context.WorkSchedules
                                    .Where(ws => ws.Day.DayOfWeek == currentDay.DayOfWeek && ws.WorkerId == targetId)
                                    .OrderByDescending(ws => ws.Day)
                                    .FirstOrDefaultAsync();

                                var (isSelected, start, end) = GetTemplateHours(template);

                                var newWorkerSchedule = new WorkScheduleModel(currentDay, isSelected, start, end, targetId);
                                context.WorkSchedules.Add(newWorkerSchedule);
                                hasChanges = true;
                            }
                        }
                    }
                }

                if (hasChanges)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation("WorkSchedule consistency check completed. Database updated.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error maintaining WorkSchedule consistency: {Message}", ex.Message);
                throw;
            }
        }

        private static (bool isSelected, TimeSpan start, TimeSpan end) GetTemplateHours(WorkScheduleModel template)
        {
            if (template != null)
            {
                return (template.IsSelected, template.Start, template.End);
            }
            return (true, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        }
    }
}