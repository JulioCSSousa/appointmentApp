namespace appointmentApp.Models.Repository.AppointmentRepository
{
    public interface IAppointmentRepository
    {
        IQueryable<Appointment> Get();
        Task<Appointment> GetById(Guid id);
        Task<Appointment?> GetByUserId(string id);
        Task<Appointment?> GetByServiceId(int id);
        Task Create(Appointment appointment);
        Task Update(Appointment appointment);
        Task Delete(Appointment appointment);
    }
}
