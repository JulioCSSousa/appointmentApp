using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using AppointmentAppMvc.Services.Stores;

namespace appointmentApp.Models.Repository.ServicesRepository
{
    public class ServiceManagerRepository : IServiceManagerRepository
    {
        private readonly AppDbContext _context;
        private readonly IStoreContext _storeContext;

        public ServiceManagerRepository(AppDbContext context, IStoreContext storeContext)
        {
            _context = context;
            _storeContext = storeContext;
        }

        public async Task<IEnumerable<ServiceModel>> GetAsync()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            return store == null
                ? Enumerable.Empty<ServiceModel>()
                : await _context.Services.Where(service => service.StoreId == store.Id).ToListAsync();
        }

        public async Task<ServiceModel> GetByIdAsync(int id)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var result = store == null
                ? null
                : await _context.Services
                    .SingleOrDefaultAsync(service => service.ServiceId == id && service.StoreId == store.Id);

            return result;
            
        }

        public async Task Create(ServiceModel service)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || service.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde ao serviço.");
            }

            try
            {
                var create = await _context.Services.AddAsync(service);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error to save Service");
            }


        }

        public async Task UpdateAsync(ServiceModel service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || service.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde ao serviço.");
            }
            if (service.ServiceName.Length < 3) throw new ArgumentException("Invalid name");
            if (service.Price < 0) throw new ArgumentException("Invalid Price");

            try
            {
                _context.Update(service);
                await _context.SaveChangesAsync();
            }
            catch (Exception) { throw new Exception("Failed to update service"); }

        }

        public async Task DeleteAsync(ServiceModel service)
        {
            if (service == null)
            {
                throw new NullReferenceException("Service not found");
            }
            var store = await _storeContext.GetCurrentStoreAsync();
            if (store == null || service.StoreId != store.Id)
            {
                throw new InvalidOperationException("A loja atual não corresponde ao serviço.");
            }
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
        }
    }
}
