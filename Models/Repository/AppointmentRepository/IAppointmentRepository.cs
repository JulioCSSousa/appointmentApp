using appointmentApp.Models.Entities;

namespace appointmentApp.Models.Repository.AppointmentRepository
{
    public interface IAppointmentRepository
    {
        IQueryable<AppointmentModel> Get();
        Task<AppointmentModel> GetById(Guid id);
        Task<AppointmentModel?> GetByUserId(string id);
        Task<AppointmentModel?> GetByServiceId(int id);
        Task Create(AppointmentModel appointment);
        Task Update(AppointmentModel appointment);
        Task Delete(AppointmentModel appointment);
    }
}
