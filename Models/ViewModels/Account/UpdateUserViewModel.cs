using System.ComponentModel.DataAnnotations;

namespace AppointmentAppMvc.Models.ViewModels.Account
{
    public class UpdateUserViewModel
    {
        public string Id { get; set; }

        [StringLength(100)]
        public string? Name { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        public string? Email { get; set; }
    }
}
