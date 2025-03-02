using appointmentApp.Models.Entities;

namespace appointmentApp.Services.WorkScheduleService
{
    public interface IWorkScheduleService
    {
        Task<IEnumerable<WorkSchedule>> GetAsync();
        Task<WorkSchedule> GetByIdAsync(int id);
        Task Create(WorkSchedule workSchedule);
        Task UpdateAsync(WorkSchedule workSchedule);
        Task DeleteAsync(WorkSchedule workSchedule);

    }
}
