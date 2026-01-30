using appointmentApp.Models.Entities;

namespace appointmentApp.Models.Repository.WorkScheduleRepository
{
    public interface IWorkScheduleRepository
    {
        IQueryable<WorkScheduleModel> GetAsync();
        Task<WorkScheduleModel> GetByIdAsync(int id);
        Task Create(WorkScheduleModel workSchedule);
        Task UpdateAsync(WorkScheduleModel workSchedule);
        Task DeleteAsync(WorkScheduleModel workSchedule);

    }
}
