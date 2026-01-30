using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace appointmentApp.Models.Repository.WorkScheduleRepository
{
    public class WorkScheduleRepository : IWorkScheduleRepository
    {
        private readonly AppDbContext _context;

        public WorkScheduleRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<WorkScheduleModel> GetAsync()
        {
            
            var result = _context.WorkSchedules.AsQueryable();
            return result;

        }

        public async Task<WorkScheduleModel> GetByIdAsync(int id)
        {
            return await _context.WorkSchedules.FindAsync(id);
        }

        public async Task Create(WorkScheduleModel workSchedule)
        {
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
            _context.WorkSchedules.Remove(workSchedule);
            await _context.SaveChangesAsync();
        }
    }
}
