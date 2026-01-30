using appointmentApp.Models.Entities;

namespace appointmentApp.Services.AppointmentServices
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentModel>> Get();
        Task<AppointmentModel> GetById(Guid id);
        Task<AppointmentModel?> GetByUserId(string id);
        Task<AppointmentModel?> GetByServiceId(int id);
        Task Create(AppointmentModel appointment);
        Task Update(AppointmentModel appointment);
        Task Delete(AppointmentModel appointment);
    }
}
