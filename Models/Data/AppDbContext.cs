
using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){ }
    
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
}