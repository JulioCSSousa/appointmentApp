using appointmentApp.Models.Repository.ServicesRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace appointmentApp.Services.ServiceManager
{
    public class ServiceManager : IServiceManager
    {
        private readonly IServiceManagerRepository _repository;

        public ServiceManager(IServiceManagerRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ServiceModel>> GetAsync(string? txt)
        {
            var dbResult = await _repository.GetAsync();
            if (!string.IsNullOrEmpty(txt))
            {
                dbResult = dbResult.Where(s => s.ServiceName.Contains(txt));
                return dbResult.ToList();
            }
            else
            {
                return dbResult.ToList();
            }
        }

        public async Task<ServiceModel> GetByIdAsync(int id)
        {

            var result = await _repository.GetByIdAsync(id);
 
            return result;
        }

        public async Task CreateAsync(ServiceModel service)
        {

            if (service.ServiceName == null || service.ServiceName.Length <= 2) throw new ArgumentException("Invalid name");
            if (service.Price < 0) throw new ArgumentException("Invalid Price");
            try
            {
                await _repository.Create(service);
            }
            catch (Exception ex)
            {
                {
                    throw new ArgumentException($"Was not possible create a new Service", ex);
                }
            }
        }

        public async Task UpdateAsync(ServiceModel service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (service.ServiceName == null || service.ServiceName.Length == 0) throw new ArgumentException("Invalid name");
            if (service.Price < 0) throw new ArgumentException("Invalid Price");
            try
            {
                await _repository.UpdateAsync(service);
            }
            catch (Exception ex)
            {
                throw new Exception("Error to update values");
            }

        }

        public async Task DeleteAsync(ServiceModel service)
        {
            if (service == null)
            {
                throw new Exception("Service not found");

            }
            await _repository.DeleteAsync(service);
        }
    }
}
