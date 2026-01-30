using appointmentApp.Models.Entities;

namespace AppointmentAppMvc.Models.ViewModels.Finance
{
    public class FinanceViewModel
    {
        public double Profits { get; set; }
        public string? ClientName { get; set; }
        public DateTime Date { get; set; }
        public string? ServiceName { get; set; }
        public double ServicePrice { get; set; }
    }
}
