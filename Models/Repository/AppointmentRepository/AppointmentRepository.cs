
using appointmentApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using AppointmentAppMvc.Services.Stores;

namespace appointmentApp.Models.Repository.AppointmentRepository
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AppDbContext _context;
        private readonly IStoreContext _storeContext;

        public AppointmentRepository(AppDbContext context, IStoreContext storeContext)
        {
            _context = context;
            _storeContext = storeContext;
        }

        public async Task<IEnumerable<AppointmentModel>> GetAsync()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            return store == null
                ? Enumerable.Empty<AppointmentModel>()
                : await _context.Appointments.Where(appointment => appointment.StoreId == store.Id).ToListAsync();
        }

        public async Task<AppointmentModel> GetById(Guid id)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            return store == null
                ? null
                : await _context.Appointments
                    .SingleOrDefaultAsync(appointment => appointment.Id == id && appointment.StoreId == store.Id);
        }
        public async Task<AppointmentModel?> GetByUserId(string id)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var result = store == null ? null : await _context.Appointments
                .Where(i => i.StoreId == store.Id && i.ClientId == id).FirstOrDefaultAsync();
            return result;
        }
        public async Task<AppointmentModel?> GetByServiceId(int id)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var result = store == null ? null : await _context.Appointments
                .Where(i => i.StoreId == store.Id && i.ServiceId == id).FirstOrDefaultAsync();
            return result;
        }
        public async Task Create(AppointmentModel appointment)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || appointment.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde ao agendamento.");
            }

            await _context.AddAsync(appointment);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(AppointmentModel appointment)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || appointment.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde ao agendamento.");
            }

            _context.Remove(appointment);
            await _context.SaveChangesAsync();
        }

        public async Task Update(AppointmentModel appointment)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || appointment.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde ao agendamento.");
            }

            _context.Update(appointment);
            _context.Entry(appointment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
