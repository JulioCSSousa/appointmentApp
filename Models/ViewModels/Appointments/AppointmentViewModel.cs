using System.ComponentModel.DataAnnotations;

namespace AppointmentAppMvc.Models.ViewModels.Appointment
{
    public class AppointmentViewModel
    {
        [Required]
        public List<ServiceModel> Services { get; set; }
        [Required]
        public int SelectedServiceId { get; set; }
        [Required]
        public string ClientId { get; set; }
        [Required]
        public DateTime SelectedDayAndHour { get; set; }
        [Required]
        public List<bool> IsDayClosed { get; set; }
        [Required]
        public Dictionary<DateTime, List<TimeSpan>> HoursToSchedule { get; set; } = new Dictionary<DateTime, List<TimeSpan>>();

    }
}
