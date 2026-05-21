
using appointmentApp.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace appointmentApp.Models.Repository.AppointmentRepository
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AppDbContext _context;

        public AppointmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AppointmentModel>> GetAsync()
        {
            return await _context.Appointments.ToListAsync();
        }

        public async Task<AppointmentModel> GetById(Guid id)
        {
            return await _context.Appointments.FindAsync(id);
        }
        public async Task<AppointmentModel?> GetByUserId(string id)
        {
            var result = await _context.Appointments.Where(i => i.ClientId == id).FirstOrDefaultAsync();
            return result;
        }
        public async Task<AppointmentModel?> GetByServiceId(int id)
        {
            var result = await _context.Appointments.Where(i => i.ServiceId == id).FirstOrDefaultAsync();
            return result;
        }
        public async Task Create(AppointmentModel appointment)
        {
            await _context.AddAsync(appointment);
            await _context.SaveChangesAsync();
            int affectedRows = await _context.SaveChangesAsync();

            
        }

        public async Task Delete(AppointmentModel appointment)
        {
            _context.Remove(appointment);
            await _context.SaveChangesAsync();
        }

        public Task Update(AppointmentModel appointment)
        {
            _context.Update(appointment);
            _context.Entry(appointment).State = EntityState.Modified;
            _context.SaveChanges();
            return Task.CompletedTask;
        }
    }
}
