using appointmentApp.Models.Entities;
using appointmentApp.Models.Repository.WorkScheduleRepository;
using Microsoft.EntityFrameworkCore;

namespace appointmentApp.Services.WorkScheduleService
{
    public class WorkScheduleService : IWorkScheduleService
    {
        private readonly IWorkScheduleRepository _repository;
        public WorkScheduleService(IWorkScheduleRepository repository) 
        {
            _repository = repository;
        }

        async Task<IEnumerable<WorkScheduleModel>> IWorkScheduleService.GetAsync()
        {
            return await _repository.GetAsync().ToListAsync();
        }
        async Task IWorkScheduleService.Create(WorkScheduleModel workSchedule)
        {

             await _repository.Create(workSchedule);

        }

        async Task IWorkScheduleService.DeleteAsync(WorkScheduleModel workSchedule)
        {
            await _repository.DeleteAsync(workSchedule);
        }

        
        async Task<WorkScheduleModel> IWorkScheduleService.GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        async Task IWorkScheduleService.UpdateAsync(WorkScheduleModel workSchedule)
        {
           await _repository.UpdateAsync(workSchedule);
        }

        
    }
}
