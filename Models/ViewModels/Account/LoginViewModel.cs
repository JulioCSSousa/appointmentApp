using System.ComponentModel.DataAnnotations;

namespace AppointmentAppMvc.Models.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

}
