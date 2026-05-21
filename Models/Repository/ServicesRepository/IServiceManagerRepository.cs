namespace appointmentApp.Models.Repository.ServicesRepository
{
    public interface IServiceManagerRepository
    {
        Task<IEnumerable<ServiceModel>> GetAsync();
        Task<ServiceModel> GetByIdAsync(int id);
        Task Create(ServiceModel service);
        Task UpdateAsync(ServiceModel service);
        Task DeleteAsync(ServiceModel service);

    }
}
