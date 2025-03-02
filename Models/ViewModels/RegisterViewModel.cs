using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "As senhas não conferem.")]
        public string ConfirmPassword { get; set; }
    }

}
