
using AppointmentAppMvc.Services.Utilities.Enum;
using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.Entities
{
    public class AppointmentModel
    {

        [Key]
        public Guid Id { get; set; }
        public Guid? StoreId { get; private set; }
        public StoreModel? Store { get; private set; }
        public string? ClientId { get; private set; }
        public string? GuestName { get; private set; }
        public string? GuestPhone { get; private set; }
        public int ServiceId { get; private set; }
        public string? WorkerId { get; private set; }
        public string ServiceName { get; set; } = string.Empty; 
        public double ServicePrice { get; set; }
        public DateTime Day { get; private set; }
        public string Status { get; private set; }

        private AppointmentModel() { }
        public AppointmentModel(Guid id, string? clientId, int serviceId, string serviceName, double servicePrice, string? workerId, DateTime day, string status, string? guestName = null, string? guestPhone = null)
        {
            Id = id;
            ClientId = clientId;
            GuestName = guestName;
            GuestPhone = guestPhone;
            ServiceId = serviceId;
            ServiceName = serviceName;
            ServicePrice = servicePrice;
            WorkerId = workerId;
            Day = day;
            Status = status;

        }

        public void SetClientId(string? id)
        {
            ClientId = id;
        }

        public void SetStore(Guid storeId)
        {
            StoreId = storeId;
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

