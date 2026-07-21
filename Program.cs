using appointmentApp.ApplicationSeed;
using appointmentApp.Models.Entities;
using appointmentApp.Models.Repository.AppointmentRepository;
using appointmentApp.Models.Repository.ServicesRepository;
using appointmentApp.Models.Repository.WorkScheduleRepository;
using appointmentApp.Services.AppointmentServices;
using appointmentApp.Services.ServiceManager;
using appointmentApp.Services.Utilities;
using appointmentApp.Services.WorkScheduleService;
using AppointmentAppMvc.Hubs;

using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Carregar variáveis de ambiente do arquivo .env
Env.Load();

// 1. Tenta ler o que o Docker Compose injetou. Se não existir, pega do seu .env local.
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? Environment.GetEnvironmentVariable("LOCAL_HOST") ?? Environment.GetEnvironmentVariable("AIVEN_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? Environment.GetEnvironmentVariable("LOCAL_PORT") ?? Environment.GetEnvironmentVariable("AIVEN_PORT");
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? Environment.GetEnvironmentVariable("LOCAL_USER") ?? Environment.GetEnvironmentVariable("AIVEN_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Environment.GetEnvironmentVariable("LOCAL_PASSWORD") ?? Environment.GetEnvironmentVariable("AIVEN_PASSWORD");
var dbName = Environment.GetEnvironmentVariable("DB") ?? Environment.GetEnvironmentVariable("LOCAL_DB") ?? Environment.GetEnvironmentVariable("AIVEN_DB");

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPassword};";

// Configurar DbContext
var serverVersion = new MySqlServerVersion(new Version(8, 0, 37)); 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 1,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )));
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddIdentity<UserModel, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Adicionar a configuração de e-mail
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender>(provider =>
{
    var emailSettings = provider.GetRequiredService<IOptions<EmailSettings>>().Value;
    return new EmailSender(
        emailSettings.SmtpHost,
        emailSettings.SmtpPort,
        emailSettings.SmtpUser,
        emailSettings.SmtpPass
    );
});

// Adicionar repositórios e serviços
builder.Services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
builder.Services.AddScoped<IWorkScheduleService, WorkScheduleService>();
builder.Services.AddScoped<IServiceManagerRepository, ServiceManagerRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IServiceManager, ServiceManager>();



builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});
builder.Services.AddSignalR();

// Construir a aplicação
var app = builder.Build();
app.UseDeveloperExceptionPage();
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/OfflineDbError");
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

app.UseHttpsRedirection();

var defaultCulture = new CultureInfo("en-US"); 

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    int retries = 10; 
    while (retries > 0)
    {
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var seedLogger = services.GetRequiredService<ILogger<SeedWorkSchedule>>();

            logger.LogInformation("--> Connecting to MySQL...");

            await context.Database.MigrateAsync();
            logger.LogInformation("--> Migrations successfull applied");

            logger.LogInformation("--> Seeds Executing...");
            // Exemplo de boa prática dentro do método de Seed:
            if (!context.WorkSchedules.Any())
            {
                // Só insere se o banco estiver vazio!
                await SeedWorkSchedule.SeedWorkSchedules(context, seedLogger);
                await context.SaveChangesAsync();
            }
            if (!context.WorkSchedules.Any())
            {
                await RoleInitializer.SeedRolesAsync(roleManager);
            }
            
            // await UserInitializer.SeedMassUsersAsync(userManager); <= populate Database with random test users

            logger.LogInformation("--> Database configured and successfully seeded!");
            break; 
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning($"--> MySQL inicialization... (Try {11 - retries}/10). Error: {ex.Message}");

            if (retries == 0)
            {
                logger.LogError(ex, "Database Seeding or Conection error.");
                throw;
            }

            await Task.Delay(5000); 
        }
    }
}

app.Run();
