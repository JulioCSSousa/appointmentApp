using appointmentApp.Models.Entities;

namespace appointmentApp.Models.Repository.WorkScheduleRepository
{
    public interface IWorkScheduleRepository
    {
        Task<IEnumerable<WorkScheduleModel>> GetAsync(DateTime? startDate, DateTime? endDate);
        Task<WorkScheduleModel> GetByIdAsync(int id);
        Task Create(WorkScheduleModel workSchedule);
        Task UpdateAsync(WorkScheduleModel workSchedule);
        Task DeleteAsync(WorkScheduleModel workSchedule);

    }
}
