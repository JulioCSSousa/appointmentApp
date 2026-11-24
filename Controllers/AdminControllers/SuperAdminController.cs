using AppointmentAppMvc.Models.ViewModels.SuperAdmin;
using appointmentApp.Constants;
using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAppMvc.Controllers.AdminControllers;

[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController : Controller
{
    private static readonly HashSet<string> AvailablePlans = new(StringComparer.OrdinalIgnoreCase)
    {
        "Trial", "Free", "Pro", "Premium"
    };

    private readonly AppDbContext _dbContext;
    private readonly UserManager<UserModel> _userManager;

    public SuperAdminController(AppDbContext dbContext, UserManager<UserModel> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var stores = await _dbContext.Stores
            .AsNoTracking()
            .Include(store => store.Tenant)
            .OrderBy(store => store.Name)
            .ToListAsync();

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => user.StoreId != null)
            .ToListAsync();

        var workerIds = (await _userManager.GetUsersInRoleAsync(Roles.Worker))
            .Select(user => user.Id)
            .ToHashSet();
        var clientIds = (await _userManager.GetUsersInRoleAsync(Roles.User))
            .Select(user => user.Id)
            .ToHashSet();

        var model = new SuperAdminDashboardViewModel
        {
            Tenants = stores.Select(store => new SuperAdminTenantViewModel
            {
                TenantId = store.TenantId,
                TenantName = store.Tenant.Name,
                TenantSlug = store.Tenant.Slug,
                StoreId = store.Id,
                StoreName = store.Name,
                StoreSlug = store.Slug,
                ActivePlan = store.ActivePlan,
                PlanStatus = store.PlanStatus,
                PlanExpiresAt = store.PlanExpiresAt,
                UserCount = users.Count(user => user.StoreId == store.Id),
                WorkerCount = users.Count(user => user.StoreId == store.Id && workerIds.Contains(user.Id)),
                ClientCount = users.Count(user => user.StoreId == store.Id && clientIds.Contains(user.Id))
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateTenantViewModel { Plan = "Trial" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTenantViewModel model)
    {
        model.StoreSlug = model.StoreSlug.Trim().ToLowerInvariant();
        model.AdminEmail = model.AdminEmail.Trim();

        if (!AvailablePlans.Contains(model.Plan))
        {
            ModelState.AddModelError(nameof(model.Plan), "Selecione um plano válido.");
        }

        if (await _dbContext.Stores.AnyAsync(store => store.Slug == model.StoreSlug))
        {
            ModelState.AddModelError(nameof(model.StoreSlug), "Este slug já está em uso.");
        }

        if (await _userManager.FindByEmailAsync(model.AdminEmail) != null)
        {
            ModelState.AddModelError(nameof(model.AdminEmail), "Este e-mail já está cadastrado.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var tenant = new TenantModel(model.TenantName.Trim(), model.StoreSlug);
        var isTrial = model.Plan.Equals("Trial", StringComparison.OrdinalIgnoreCase);
        DateTime? expiresAt = isTrial ? DateTime.UtcNow.AddDays(14) : null;
        var store = new StoreModel(
            tenant.Id,
            model.StoreName.Trim(),
            model.StoreSlug,
            model.Plan,
            isTrial ? "Trial" : "Active",
            expiresAt);
        tenant.SetStore(store);

        IdentityResult? adminCreationResult = null;
        try
        {
            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                _dbContext.Tenants.Add(tenant);
                _dbContext.Stores.Add(store);
                await _dbContext.SaveChangesAsync();

                var admin = new UserModel(model.AdminName.Trim(), model.AdminEmail, model.AdminPhoneNumber)
                {
                    UserName = model.AdminEmail,
                    Email = model.AdminEmail,
                    EmailConfirmed = true
                };
                admin.SetStore(store.Id);

                adminCreationResult = await _userManager.CreateAsync(admin, model.AdminPassword);
                if (!adminCreationResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var roleResult = await _userManager.AddToRoleAsync(admin, Roles.Admin);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Não foi possível atribuir o papel Admin: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
                }

                await transaction.CommitAsync();
            });

            if (adminCreationResult is not null && !adminCreationResult.Succeeded)
            {
                foreach (var error in adminCreationResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] = "Tenant criado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Não foi possível criar o tenant. Tente novamente.");
            return View(model);
        }
    }
}
