using appointmentApp.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace AppointmentAppMvc.ApplicationSeed
{
    public class UserInitializer
    {
        public static async Task SeedMassUsersAsync(UserManager<UserModel> userManager)
        {
            var nomes = new[] { "Gabriel", "Lucas", "Rafael", "Ana", "Beatriz", "Marcos", "Julian", "Erik", "Caio", "Larissa" };
            var sobrenomes = new[] { "Silva", "Santos", "Oliveira", "Souza", "Pereira", "Ferreira", "Almeida" };

            var random = new Random();

            for (int i = 1; i <= 100; i++)
            {
                var nomeAleatorio = nomes[random.Next(nomes.Length)];
                var sobrenomeAleatorio = sobrenomes[random.Next(sobrenomes.Length)];
                var email = $"{nomeAleatorio.ToLower()}.{sobrenomeAleatorio.ToLower()}{i}@teste.com";

                var existingUser = await userManager.FindByEmailAsync(email);

                if (existingUser == null)
                {
                    var user = new UserModel
                    (
                        nomeAleatorio,
                        email,
                        "119" + random.Next(10000000, 99999999).ToString()
                    );

                    // Criando com uma senha padrão para todos os testes
                    var result = await userManager.CreateAsync(user, "159753");

                    if (result.Succeeded)
                    {
                        // Todos entram como Clientes para testar a sua lógica de Roles
                        await userManager.AddToRoleAsync(user, appointmentApp.Constants.Roles.User);
                    }
                }
            }
        }
    }
}
