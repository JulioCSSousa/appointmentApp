using AppointmentAppMvc.Models.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.Entities
{
    
    public class UserModel : IdentityUser
    {

        [Required]
        public string Name { get; protected set; }

        protected UserModel() { }
        
        public UserModel(string name, string email, string phoneNumber)
        {
            Name = name;
            Email = email;
            NormalizedEmail = email.ToUpper(); 
            UserName = email;                 
            NormalizedUserName = email.ToUpper();
            PhoneNumber = phoneNumber;
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public static async Task<List<UserWithRolesViewModel>> UserWithRolesAsync(UserManager<UserModel> _userManager, string? searchTerm = null, int page = 1)
        {
            int itemByPge = 10;
            int skip = (page - 1) * itemByPge;

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            int totalItems = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Name)
                .Skip(skip)
                .Take(itemByPge)
                .ToListAsync();

            List<UserWithRolesViewModel> usersWithRoles = new List<UserWithRolesViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new UserWithRolesViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? "Sem Role"
                });
            }

            return usersWithRoles;
        }

    }
}
