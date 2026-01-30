using System.ComponentModel.DataAnnotations;

namespace AppointmentAppMvc.Models.ViewModels.Account
{
    public class PasswordRecoverViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
