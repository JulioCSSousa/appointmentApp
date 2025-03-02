
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
        public async Task  Create(Appointment appointment)
        {
            var existingAppointment = await _repository.GetByUserId(appointment.ClientId);
            if (existingAppointment != null)
            {
                // Lança uma exceção com a mensagem apropriada
                throw new InvalidOperationException("Já existe um agendamento para este cliente.");
            }

            // Caso contrário, cria o novo agendamento
            await _repository.Create(appointment);
        }


        public async Task Delete(Appointment appointment)
        {
            await _repository.Delete(appointment);
        }

        public async Task<IEnumerable<Appointment>> Get()
        {
            return await _repository.Get().ToListAsync();
        }

        public async Task<Appointment> GetById(Guid id)
        {
            return await _repository.GetById(id);
        }
        public async Task<Appointment?> GetByUserId(string id)
        {
            return await _repository.GetByUserId(id);

        }
        public async Task<Appointment?> GetByServiceId(int id)
        {
            return await _repository.GetByServiceId(id);

        }
        public async Task Update(Appointment appointment)
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
