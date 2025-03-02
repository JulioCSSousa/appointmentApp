using appointmentApp.Models.Entities;

namespace appointmentApp.Models.ViewModels
{
    public class ClientIndexViewModel
    {
        public User? User { get; set; }
        public Appointment? Appointment { get; set; }
        public Service? Service { get; set; }
    }
}
