using AppointmentAppMvc.Services.Utilities.Enum;

namespace AppointmentAppMvc.Models.ViewModels.Appointment
{
    public class AppointmentUpdate
    {
        public Status Status { get; private set; }
        public string StoreId { get; set; }
    }
}
