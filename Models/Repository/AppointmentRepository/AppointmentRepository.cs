
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

        public IQueryable<Appointment> Get()
        {
            return _context.Appointments;
        }

        public async Task<Appointment> GetById(Guid id)
        {
            return await _context.Appointments.FindAsync(id);
        }
        public async Task<Appointment?> GetByUserId(string id)
        {
            var result = await _context.Appointments.Where(i => i.ClientId == id).FirstOrDefaultAsync();
            return result;
        }
        public async Task<Appointment?> GetByServiceId(int id)
        {
            var result = await _context.Appointments.Where(i => i.ServiceId == id).FirstOrDefaultAsync();
            return result;
        }
        public async Task Create(Appointment appointment)
        {
            await _context.AddAsync(appointment);
            await _context.SaveChangesAsync();
            int affectedRows = await _context.SaveChangesAsync();

            
        }

        public async Task Delete(Appointment appointment)
        {
            _context.Remove(appointment);
            await _context.SaveChangesAsync();
        }

        public Task Update(Appointment appointment)
        {
            _context.Update(appointment);
            _context.Entry(appointment).State = EntityState.Modified;
            _context.SaveChanges();
            return Task.CompletedTask;
        }
    }
}
