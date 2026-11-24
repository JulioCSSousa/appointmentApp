
using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AppointmentAppMvc.Services.Stores;

public class AppDbContext : IdentityDbContext<UserModel>
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public Guid? CurrentStoreId => _tenantContext.StoreId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<UserModel>()
            .HasIndex(user => user.PhoneNumberNormalized)
            .IsUnique();

        builder.Entity<TenantModel>()
            .HasIndex(tenant => tenant.Slug)
            .IsUnique();

        builder.Entity<StoreModel>()
            .HasIndex(store => store.Slug)
            .IsUnique();

        builder.Entity<StoreModel>()
            .HasOne(store => store.Tenant)
            .WithOne(tenant => tenant.Store)
            .HasForeignKey<StoreModel>(store => store.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserModel>()
            .HasOne(user => user.Store)
            .WithMany(store => store.Users)
            .HasForeignKey(user => user.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AppointmentModel>()
            .HasOne(appointment => appointment.Store)
            .WithMany(store => store.Appointments)
            .HasForeignKey(appointment => appointment.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceModel>()
            .HasOne(service => service.Store)
            .WithMany(store => store.Services)
            .HasForeignKey(service => service.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkScheduleModel>()
            .HasOne(schedule => schedule.Store)
            .WithMany(store => store.WorkSchedules)
            .HasForeignKey(schedule => schedule.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AppointmentModel>()
            .HasQueryFilter(appointment =>
                CurrentStoreId.HasValue && appointment.StoreId == CurrentStoreId);

        builder.Entity<ServiceModel>()
            .HasQueryFilter(service =>
                CurrentStoreId.HasValue && service.StoreId == CurrentStoreId);

        builder.Entity<WorkScheduleModel>()
            .HasQueryFilter(schedule =>
                CurrentStoreId.HasValue && schedule.StoreId == CurrentStoreId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateTenantChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTenantChanges()
    {
        if (CurrentStoreId is not Guid currentStoreId)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries()
                     .Where(entry => entry.State is EntityState.Added
                         or EntityState.Modified
                         or EntityState.Deleted)
                     .Where(entry => entry.Metadata.FindProperty("StoreId") != null))
        {
            var storeId = entry.Property("StoreId").CurrentValue as Guid?;
            if (storeId != currentStoreId)
            {
                throw new InvalidOperationException(
                    "A entidade não pertence à loja atual.");
            }
        }
    }
    
    public DbSet<AppointmentModel> Appointments { get; set; }
    public DbSet<ServiceModel> Services { get; set; }
    public DbSet<WorkScheduleModel> WorkSchedules { get; set; }
    public DbSet<TenantModel> Tenants { get; set; }
    public DbSet<StoreModel> Stores { get; set; }
}