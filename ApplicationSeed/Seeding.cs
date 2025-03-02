using Microsoft.AspNetCore.Identity;

public class ApplicationSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "Admin","Worker", "Client" };

        foreach (var role in roles)
        {
            var roleExist = await roleManager.RoleExistsAsync(role);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
