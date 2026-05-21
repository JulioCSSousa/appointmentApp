
using AppointmentAppMvc.Services.Utilities.Enum;
using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.Entities
{
    public class AppointmentModel
    {

        [Key]
        public Guid Id { get; set; }
        public string ClientId { get; private set; }
        public int ServiceId { get; private set; }
        public string? WorkerId { get; private set; }
        
        public DateTime Day { get; private set; }
        public string Status { get; private set; }

        private AppointmentModel() { }
        public AppointmentModel(Guid id, string clientId, int serviceId, string? workerId, DateTime day, string status)
        {
            Id = id;
            ClientId = clientId;
            ServiceId = serviceId;
            WorkerId = workerId;
            Day = day;
            Status = status;
        }

        public void SetClientId(string id)
        {
            ClientId = id;
        }
        public void SetServiceId(int serviceId)
        {
            ServiceId = serviceId;
        }
        public void SetDay(DateTime day)
        {
            Day = day;
        }

        public void SetStatus(Status status)
        {
            Status = status.ToString();
        }

        public void SetWorkerId(Guid id)
        {
            WorkerId = id.ToString();
        }
    }
}

