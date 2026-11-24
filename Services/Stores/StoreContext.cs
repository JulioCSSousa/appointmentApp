using appointmentApp.Models.Entities;
using appointmentApp.Constants;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AppointmentAppMvc.Services.Stores;

public sealed class StoreContext : IStoreContext
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;

    public StoreContext(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public async Task<StoreModel?> GetCurrentStoreAsync()
    {
        if (_tenantContext.StoreId is Guid resolvedStoreId)
        {
            return await _dbContext.Stores
                .Include(store => store.Tenant)
                .SingleOrDefaultAsync(store => store.Id == resolvedStoreId);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var slug = httpContext?.Request.RouteValues["storeSlug"]?.ToString();

        if (!string.IsNullOrWhiteSpace(slug))
        {
            var store = await _dbContext.Stores
                .Include(store => store.Tenant)
                .SingleOrDefaultAsync(store => store.Slug == slug);

            if (store == null)
            {
                _tenantContext.Clear();
                return null;
            }

            var currentUser = httpContext?.User;
            if (currentUser?.Identity?.IsAuthenticated == true &&
                !currentUser.IsInRole(Roles.SuperAdmin))
            {
                var authenticatedUserId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
                var userStoreId = await _dbContext.Users
                    .Where(user => user.Id == authenticatedUserId)
                    .Select(user => user.StoreId)
                    .SingleOrDefaultAsync();

                if (userStoreId != store.Id)
                {
                    _tenantContext.Clear();
                    return null;
                }
            }

            _tenantContext.SetStore(store.Id);
            return store;
        }

        var userId = httpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId);
            if (user?.StoreId is Guid userStoreId)
            {
                var store = await _dbContext.Stores
                    .Include(store => store.Tenant)
                    .SingleOrDefaultAsync(store => store.Id == userStoreId);

                if (store != null)
                {
                    _tenantContext.SetStore(store.Id);
                }

                return store;
            }
        }

        _tenantContext.Clear();
        return null;
    }
}
