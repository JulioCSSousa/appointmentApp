using appointmentApp.Models.Entities;

namespace appointmentApp.Models.Repository.WorkScheduleRepository
{
    public interface IWorkScheduleRepository
    {
        IQueryable<WorkSchedule> GetAsync();
        Task<WorkSchedule> GetByIdAsync(int id);
        Task Create(WorkSchedule workSchedule);
        Task UpdateAsync(WorkSchedule workSchedule);
        Task DeleteAsync(WorkSchedule workSchedule);

    }
}
