using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.ViewModels
{
    public class PasswordRecoverViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
