namespace appointmentApp.Services.ServiceManager
{
    public interface IServiceManager
    {
        Task<IEnumerable<ServiceModel>> GetAsync(string? txt);
        Task<ServiceModel> GetByIdAsync(int id);
        Task CreateAsync(ServiceModel service);
        Task UpdateAsync(ServiceModel Service);
        Task DeleteAsync(ServiceModel Service);
    }
}
