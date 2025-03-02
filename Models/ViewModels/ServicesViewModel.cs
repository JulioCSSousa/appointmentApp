using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace appointmentApp.Models.ViewModels
{
    public class ServiceViewModel
    {
        public int ServiceId { get; set; }

        [StringLength(50)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public string ServiceName { get; set; }

        [DataType(DataType.Currency)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
        public double Price { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? Duration { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

    }

}
