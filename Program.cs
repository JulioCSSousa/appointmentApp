using appointmentApp.ApplicationSeed;
using appointmentApp.Models.Entities;
using appointmentApp.Models.Repository.AppointmentRepository;
using appointmentApp.Models.Repository.ServicesRepository;
using appointmentApp.Models.Repository.WorkScheduleRepository;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using appointmentApp.Services.Utilities;
using appointmentApp.Services.WorkScheduleService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders(); // Remove provedores padr�o (opcional)
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

builder.Logging.SetMinimumLevel(LogLevel.Information);
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
new MySqlServerVersion(new Version(8, 0, 40))));


// Adicionar o ASP.NET Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Configura��es de senha sem complexidade
    options.Password.RequireDigit = false; // N�o exige n�mero
    options.Password.RequireLowercase = false; // N�o exige letra min�scula
    options.Password.RequireUppercase = false; // N�o exige letra mai�scula
    options.Password.RequireNonAlphanumeric = false; // N�o exige caracteres especiais
    options.Password.RequiredLength = 6; // Exige no m�nimo 6 caracteres
    options.Password.RequiredUniqueChars = 1; // Exige no m�nimo 1 caractere �nico
})
       .AddEntityFrameworkStores<AppDbContext>()
       .AddDefaultTokenProviders();

// Configurar autentica��o por cookies

builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login"; // Redireciona para a tela de login
        options.AccessDeniedPath = "/Account/AccessDenied"; // Redireciona para uma p�gina de acesso negado
    });
builder.Services.Configure<EmailSettings>(
builder.Configuration.GetSection("EmailSettings"));


builder.Services.AddTransient<IEmailSender>(provider =>
{
    // Obt�m os valores de configura��o de EmailSettings
    var emailSettings = provider.GetRequiredService<IOptions<EmailSettings>>().Value;

    // Retorna uma inst�ncia de EmailSender configurada
    return new EmailSender(
        emailSettings.SmtpHost,
        emailSettings.SmtpPort,
        emailSettings.SmtpUser,
        emailSettings.SmtpPass
    );
});

builder.Services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
builder.Services.AddScoped<IWorkScheduleService, WorkScheduleService>();
builder.Services.AddScoped<IServiceManagerRepository, ServiceManagerRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IServiceManager, ServiceManager>();



var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Outros c�digos de configura��o...


}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Obter o contexto do banco e o logger
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<SeedWorkSchedule>>();

    await SeedWorkSchedule.SeedWorkSchedules(context, logger);
}

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleInitializer.SeedRolesAsync(roleManager);
}

app.UseHttpsRedirection();
app.UseRouting();

builder.Services.AddAuthorization();

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");


app.Run();
