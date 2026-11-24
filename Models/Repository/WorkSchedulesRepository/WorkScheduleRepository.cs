using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using AppointmentAppMvc.Services.Stores;

namespace appointmentApp.Models.Repository.WorkScheduleRepository
{
    public class WorkScheduleRepository : IWorkScheduleRepository
    {
        private readonly AppDbContext _context;
        private readonly IStoreContext _storeContext;

        public WorkScheduleRepository(AppDbContext context, IStoreContext storeContext)
        {
            _context = context;
            _storeContext = storeContext;
        }

        public async Task<IEnumerable<WorkScheduleModel>> GetAsync(DateTime? startDate, DateTime? endDate)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var result = store == null
                ? new List<WorkScheduleModel>()
                : await _context.WorkSchedules
                    .Where(schedule => schedule.StoreId == store.Id)
                    .ToListAsync();

            if (startDate != null || endDate != null)
            {
                result = result.Where(d => d.Day >= startDate && d.Day <= endDate).ToList();
            }
            

            return result;

        }

        public async Task<WorkScheduleModel> GetByIdAsync(int id)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            return store == null
                ? null
                : await _context.WorkSchedules
                    .SingleOrDefaultAsync(schedule => schedule.Id == id && schedule.StoreId == store.Id);
        }

        public async Task Create(WorkScheduleModel workSchedule)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || workSchedule.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde à escala.");
            }

            try
            {
                await _context.WorkSchedules.AddAsync(workSchedule);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error to save Schedule");
            }
        }

        public async Task UpdateAsync(WorkScheduleModel workSchedule)
        {
            if (workSchedule == null) throw new ArgumentNullException(nameof(workSchedule));
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || workSchedule.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde à escala.");
            }

            try
            {
                _context.Update(workSchedule);
                _context.Entry(workSchedule).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception) { throw new Exception("Failed to update"); }

        }

        public async Task DeleteAsync(WorkScheduleModel workSchedule)
        {
            if (workSchedule == null)
            {
                throw new NullReferenceException("Schedule not found");
            }
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || workSchedule.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde à escala.");
            }
            _context.WorkSchedules.Remove(workSchedule);
            await _context.SaveChangesAsync();
        }
    }
}
