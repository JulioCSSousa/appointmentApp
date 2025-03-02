using appointmentApp.Models.Entities;

namespace appointmentApp.Models.ViewModels
{
        public class UserWithRolesViewModel : User
        {
        public string Name { get; set; }
        public string? Role { get; set; }
        }
}
