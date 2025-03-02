using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.ViewModels
{
    public class CreateServiceViewModel
    {
        [StringLength(50)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public string ServiceName { get; set; }

        [Required(ErrorMessage = "Este campo é obrigatório")]
        [DataType(DataType.Currency)]
        public double Price { get; set; }

        [Required(ErrorMessage = "Este campo é obrigatório")]
        [DataType(DataType.Time)]
        public TimeSpan Duration { get; set; }

        [Required(ErrorMessage = "Este campo é obrigatório")]
        [StringLength(50)]
        public string Category { get; set; }

    }

}
