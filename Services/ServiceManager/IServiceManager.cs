namespace appointmentApp.Services.ServiceManager
{
    public interface IServiceManager
    {
        Task<IEnumerable<Service>> GetAsync(string? txt);
        Task<Service> GetByIdAsync(int id);
        Task CreateAsync(Service service);
        Task UpdateAsync(Service Service);
        Task DeleteAsync(Service Service);
    }
}
