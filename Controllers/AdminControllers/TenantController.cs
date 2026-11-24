using appointmentApp.Models.Entities;
using AppointmentAppMvc.Models.ViewModels.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AppointmentAppMvc.Controllers.AdminControllers;

[Authorize(Roles = "Admin")]
public class TenantController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<UserModel> _userManager;

    public TenantController(AppDbContext dbContext, UserManager<UserModel> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await LoadDashboardAsync();
        return model == null ? NotFound("Nenhuma loja foi associada ao administrador.") : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(TenantDashboardViewModel model)
    {
        var current = await LoadDashboardAsync();
        if (current == null)
        {
            return NotFound("Nenhuma loja foi associada ao administrador.");
        }

        if (!ModelState.IsValid)
        {
            model.TenantId = current.TenantId;
            model.StoreId = current.StoreId;
            model.Workers = current.Workers;
            model.Clients = current.Clients;
            return View("Index", model);
        }

        var store = await _dbContext.Stores
            .Include(item => item.Tenant)
            .SingleOrDefaultAsync(item => item.Id == current.StoreId);

        if (store == null || store.TenantId != current.TenantId)
        {
            return NotFound("Loja não encontrada.");
        }

        var normalizedSlug = model.StoreSlug.Trim().ToLowerInvariant();
        var slugInUse = await _dbContext.Stores
            .AnyAsync(item => item.Slug == normalizedSlug && item.Id != store.Id);

        if (slugInUse)
        {
            ModelState.AddModelError(nameof(model.StoreSlug), "Este slug já está sendo usado por outra loja.");
            model.TenantId = current.TenantId;
            model.StoreId = current.StoreId;
            model.Workers = current.Workers;
            model.Clients = current.Clients;
            return View("Index", model);
        }

        store.UpdateDetails(model.StoreName, normalizedSlug);
        store.Tenant.SetName(model.TenantName);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Dados da loja atualizados com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<TenantDashboardViewModel?> LoadDashboardAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var admin = await _userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId);

        if (admin?.StoreId is not Guid storeId)
        {
            return null;
        }

        var store = await _dbContext.Stores
            .AsNoTracking()
            .Include(item => item.Tenant)
            .SingleOrDefaultAsync(item => item.Id == storeId);

        if (store == null)
        {
            return null;
        }

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => user.StoreId == storeId && user.Id != admin.Id)
            .OrderBy(user => user.Name)
            .ToListAsync();

        var workers = new List<TenantUserViewModel>();
        var clients = new List<TenantUserViewModel>();
        foreach (var user in users)
        {
            var item = new TenantUserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Worker"))
            {
                workers.Add(item);
            }
            else if (roles.Contains("Client"))
            {
                clients.Add(item);
            }
        }

        return new TenantDashboardViewModel
        {
            TenantId = store.TenantId,
            StoreId = store.Id,
            TenantName = store.Tenant.Name,
            StoreName = store.Name,
            StoreSlug = store.Slug,
            Workers = workers,
            Clients = clients
        };
    }
}
