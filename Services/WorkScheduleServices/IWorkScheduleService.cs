using appointmentApp.Models.Entities;

namespace appointmentApp.Services.WorkScheduleService
{
    public interface IWorkScheduleService
    {
        Task<IEnumerable<WorkScheduleModel>> GetAsync();
        Task<WorkScheduleModel> GetByIdAsync(int id);
        Task Create(WorkScheduleModel workSchedule);
        Task UpdateAsync(WorkScheduleModel workSchedule);
        Task DeleteAsync(WorkScheduleModel workSchedule);

    }
}
