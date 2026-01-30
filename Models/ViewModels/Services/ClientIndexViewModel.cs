
using appointmentApp.Models.Entities;

namespace AppointmentAppMvc.Models.ViewModels.Get
{
    public class ClientIndexViewModel
    {
        public UserModel? User { get; set; }
        public AppointmentModel? Appointment { get; set; }
        public ServiceModel? Service { get; set; }
    }
}
