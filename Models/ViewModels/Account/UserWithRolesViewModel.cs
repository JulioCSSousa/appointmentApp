using appointmentApp.Models.Entities;

namespace AppointmentAppMvc.Models.ViewModels.Account
{
        public class UserWithRolesViewModel : UserModel
        {
        public string Name { get; set; }
        public string? Role { get; set; }
        }
}
