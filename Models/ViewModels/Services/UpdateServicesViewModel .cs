using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AppointmentAppMvc.Models.ViewModels.Services
{
    public class UpdateServiceViewModel
    {
        public int ServiceId { get; set; }

        [StringLength(50)]
        public string? ServiceName { get; set; }

        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
        public double? Price { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? Duration { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

    }

}
