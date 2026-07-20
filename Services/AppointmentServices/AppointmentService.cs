
using appointmentApp.Models.Entities;
using appointmentApp.Models.Repository.AppointmentRepository;
using Microsoft.EntityFrameworkCore;


namespace appointmentApp.Services.AppointmentServices
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        public AppointmentService(IAppointmentRepository repository) 
        {
            _repository = repository;
        }
        public async Task  Create(AppointmentModel appointment)
        {
            /*var existingAppointment = await _repository.GetByUserId(appointment.ClientId);
            if (existingAppointment != null)
            {
                // Lança uma exceção com a mensagem apropriada
                throw new InvalidOperationException("Appointment alread existis for this client.");
            }*/

            await _repository.Create(appointment);
        }


        public async Task Delete(AppointmentModel appointment)
        {
            await _repository.Delete(appointment);
        }

        public async Task<IEnumerable<AppointmentModel>> Get()
        {
            return await _repository.GetAsync();
        }

        public async Task<AppointmentModel> GetById(Guid id)
        {
            return await _repository.GetById(id);
        }
        public async Task<AppointmentModel?> GetByUserId(string id)
        {
            return await _repository.GetByUserId(id);

        }
        public async Task<AppointmentModel?> GetByServiceId(int id)
        {
            return await _repository.GetByServiceId(id);

        }
        public async Task Update(AppointmentModel appointment)
        {
            var result = _repository.GetById(appointment.Id);
            if (result == null)
            {
                throw new NullReferenceException("Id not exists");
            }
            await _repository.Update(appointment);
        }
    }
}
