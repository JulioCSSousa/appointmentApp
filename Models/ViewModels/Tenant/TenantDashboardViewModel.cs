using System.ComponentModel.DataAnnotations;

namespace AppointmentAppMvc.Models.ViewModels.Tenant;

public class TenantDashboardViewModel
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }

    [Display(Name = "Nome do tenant")]
    [Required(ErrorMessage = "Informe o nome do tenant.")]
    [StringLength(120)]
    public string TenantName { get; set; } = string.Empty;

    [Display(Name = "Nome da loja")]
    [Required(ErrorMessage = "Informe o nome da loja.")]
    [StringLength(120)]
    public string StoreName { get; set; } = string.Empty;

    [Display(Name = "Slug público")]
    [Required(ErrorMessage = "Informe o slug da loja.")]
    [StringLength(80)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use apenas letras minúsculas, números e hífens.")]
    public string StoreSlug { get; set; } = string.Empty;

    public List<TenantUserViewModel> Workers { get; set; } = new();
    public List<TenantUserViewModel> Clients { get; set; } = new();
}

public class TenantUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}
