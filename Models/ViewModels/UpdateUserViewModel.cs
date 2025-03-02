using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.ViewModels
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
