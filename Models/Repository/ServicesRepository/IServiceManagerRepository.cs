namespace appointmentApp.Models.Repository.ServicesRepository
{
    public interface IServiceManagerRepository
    {
        IQueryable<Service> GetAsync();
        Task<Service> GetByIdAsync(int id);
        Task Create(Service service);
        Task UpdateAsync(Service service);
        Task DeleteAsync(Service service);

    }
}
