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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);


Env.Load();

// 1. connection strings
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? Environment.GetEnvironmentVariable("LOCAL_HOST") ?? Environment.GetEnvironmentVariable("AIVEN_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? Environment.GetEnvironmentVariable("LOCAL_PORT") ?? Environment.GetEnvironmentVariable("AIVEN_PORT");
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? Environment.GetEnvironmentVariable("LOCAL_USER") ?? Environment.GetEnvironmentVariable("AIVEN_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Environment.GetEnvironmentVariable("LOCAL_PASSWORD") ?? Environment.GetEnvironmentVariable("AIVEN_PASSWORD");
var dbName = Environment.GetEnvironmentVariable("DB") ?? Environment.GetEnvironmentVariable("LOCAL_DB") ?? Environment.GetEnvironmentVariable("AIVEN_DB");

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPassword};";


var serverVersion = new MySqlServerVersion(new Version(8, 0, 37));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )));


builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"./temp-keys"));

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

// login routes
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "AppointmentApp_Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// email config
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

// repositories
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

// ----------------------------------------------------------------------
// BUILD and MIDDLEWARES
// ----------------------------------------------------------------------
var app = builder.Build();

// 5. Configuração de Proxy/Headers para o Render
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/OfflineDbError");
    app.UseHsts();
}

app.UseStaticFiles();

// 6. Find Coins
var defaultCulture = new CultureInfo("en-US");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

// 7. Seeds
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
            logger.LogInformation("--> Migrations successfully applied");

            logger.LogInformation("--> Seeds Executing...");
            if (!context.WorkSchedules.Any())
            {
                await SeedWorkSchedule.SeedWorkSchedules(context, seedLogger);
                await context.SaveChangesAsync();
            }
            if (!await roleManager.Roles.AnyAsync())
            {
                await RoleInitializer.SeedRolesAsync(roleManager);
            }

            logger.LogInformation("--> Database configured and successfully seeded!");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning($"--> MySQL initialization... (Try {11 - retries}/10). Error: {ex.Message}");

            if (retries == 0)
            {
                logger.LogError(ex, "Database Seeding or Connection error.");
                throw;
            }

            await Task.Delay(5000);
        }
    }
}

app.Run();