using appointmentApp.Constants;
using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace AppointmentAppMvc.ApplicationSeed;

public static class SuperAdminInitializer
{
    public static async Task SeedAsync(UserManager<UserModel> userManager, StoreModel defaultStore)
    {
        var email = Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new UserModel("Super Admin", email, "00000000000")
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            user.SetStore(defaultStore.Id);

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Não foi possível criar o SuperAdmin: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, Roles.SuperAdmin))
        {
            await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        }
    }
}