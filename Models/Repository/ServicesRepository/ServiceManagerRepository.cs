using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace appointmentApp.Models.Repository.ServicesRepository
{
    public class ServiceManagerRepository : IServiceManagerRepository
    {
        private readonly AppDbContext _context;

        public ServiceManagerRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<ServiceModel> GetAsync()
        {
            return _context.Services;
        }

        public async Task<ServiceModel> GetByIdAsync(int id)
        {
            var result = await _context.Services.FindAsync(id);

            return result;
            
        }

        public async Task Create(ServiceModel service)
        {
            try
            {
                var create = _context.Services.AddAsync(service);
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
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
        }
    }
}
