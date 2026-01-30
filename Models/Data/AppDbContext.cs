
using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<UserModel>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){ }
    
    public DbSet<AppointmentModel> Appointments { get; set; }
    public DbSet<ServiceModel> Services { get; set; }
    public DbSet<WorkScheduleModel> WorkSchedules { get; set; }
}