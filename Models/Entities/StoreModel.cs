using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.Entities;

public class StoreModel
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Slug { get; private set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string ActivePlan { get; private set; } = "Free";

    [Required]
    [MaxLength(40)]
    public string PlanStatus { get; private set; } = "Active";

    public DateTime? PlanExpiresAt { get; private set; }

    public Guid TenantId { get; private set; }
    public TenantModel Tenant { get; private set; } = null!;

    public ICollection<UserModel> Users { get; private set; } = new List<UserModel>();
    public ICollection<AppointmentModel> Appointments { get; private set; } = new List<AppointmentModel>();
    public ICollection<ServiceModel> Services { get; private set; } = new List<ServiceModel>();
    public ICollection<WorkScheduleModel> WorkSchedules { get; private set; } = new List<WorkScheduleModel>();

    private StoreModel() { }

    public StoreModel(Guid tenantId, string name, string slug, string activePlan = "Free", string planStatus = "Active", DateTime? planExpiresAt = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name;
        Slug = slug;
        ActivePlan = activePlan;
        PlanStatus = planStatus;
        PlanExpiresAt = planExpiresAt;
    }

    public void UpdateDetails(string name, string slug)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
    }

    public void UpdatePlan(string activePlan, string planStatus, DateTime? planExpiresAt)
    {
        ActivePlan = activePlan.Trim();
        PlanStatus = planStatus.Trim();
        PlanExpiresAt = planExpiresAt;
    }
}
