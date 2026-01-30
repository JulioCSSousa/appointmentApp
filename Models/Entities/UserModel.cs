using Microsoft.AspNetCore.Identity;
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
            NormalizedEmail = email.ToUpper(); // Boa prática setar a normalização
            UserName = email;                 // ISSO ESTAVA FALTANDO
            NormalizedUserName = email.ToUpper();
            PhoneNumber = phoneNumber;
        }

        public void SetName(string name)
        {
            Name = name;
        }

    }
}
