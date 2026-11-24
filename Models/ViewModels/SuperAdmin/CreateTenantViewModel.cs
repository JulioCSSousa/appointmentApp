using System.ComponentModel.DataAnnotations;

namespace AppointmentAppMvc.Models.ViewModels.SuperAdmin;

public class CreateTenantViewModel
{
    [Required(ErrorMessage = "Informe o nome do tenant.")]
    [StringLength(120)]
    [Display(Name = "Nome do tenant")]
    public string TenantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o nome da loja.")]
    [StringLength(120)]
    [Display(Name = "Nome da loja")]
    public string StoreName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o slug da loja.")]
    [StringLength(80)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use apenas letras minúsculas, números e hífens.")]
    [Display(Name = "Slug público")]
    public string StoreSlug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail do administrador.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [Display(Name = "E-mail do administrador")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o nome do administrador.")]
    [StringLength(120)]
    [Display(Name = "Nome do administrador")]
    public string AdminName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone do administrador.")]
    [Display(Name = "Telefone do administrador")]
    public string AdminPhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha do administrador.")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Senha inicial")]
    public string AdminPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha um plano.")]
    [Display(Name = "Plano")]
    public string Plan { get; set; } = "Trial";
}
