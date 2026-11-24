namespace AppointmentAppMvc.Models.ViewModels.SuperAdmin;

public class SuperAdminDashboardViewModel
{
    public List<SuperAdminTenantViewModel> Tenants { get; set; } = new();
}

public class SuperAdminTenantViewModel
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
    public string ActivePlan { get; set; } = string.Empty;
    public string PlanStatus { get; set; } = string.Empty;
    public DateTime? PlanExpiresAt { get; set; }
    public int UserCount { get; set; }
    public int WorkerCount { get; set; }
    public int ClientCount { get; set; }
}
