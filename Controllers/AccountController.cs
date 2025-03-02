using appointmentApp.Models.Entities;
using appointmentApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailSender _emailSender;

    public AccountController(UserManager<User> userManager, 
        SignInManager<User> signInManager, 
        RoleManager<IdentityRole> roleManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register() => View();
    
    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        
        if (ModelState.IsValid)
        {
            var users = await _userManager.Users.AnyAsync();

            if (!users)
            {
                var user = new User(model.Name, model.Email, model.PhoneNumber)
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                await _userManager.AddToRoleAsync(user, "Admin");
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Account");

                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

            }
            else
            {
                var user = new User(model.Name, model.Email, model.PhoneNumber)
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                await _userManager.AddToRoleAsync(user, "Cliente");
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Account");

                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        return View(model); 
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login() => View();

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user == null)
                {
                    return BadRequest();
                }
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.ToList();

                if(roles == null)
                {
                    return BadRequest("Role não encontrado");
                }

                if(roles.FirstOrDefault("Admin") == "Admin")
                {
                    return RedirectToAction("IndexAdm", "Home");
      
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Login inválido.");
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> PasswordRecovery()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasswordRecovery([FromForm] string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Não expor se o e-mail está correto ou não por motivos de segurança
            return RedirectToAction("Login", "Account");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action(
            "PasswordReset",
            "Account",
            new { token = token, email = user.Email },
            protocol: Request.Scheme);

        // Envia o link de recuperação de senha para o e-mail do usuário
        await _emailSender.SendEmailAsync(
            email,
            "Redefinição de Senha",
            $"Clique aqui para redefinir sua senha: <a href='{callbackUrl}'>Redefinir Senha</a>");

        return RedirectToAction("PasswordRecoveryConfirmation", "Account");
    }

    [HttpGet]
    public async Task<IActionResult> PasswordRecoveryConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult PasswordReset(string email, string token)
    {
        if (token == null || email == null)
        {
            return BadRequest("Token inválido.");
        }

        var model = new PasswordResetViewModel { Token = token, Email = email };
        return View(model);
    }

[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PasswordReset(PasswordResetViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var user = await _userManager.FindByEmailAsync(model.Email);
    if (user == null)
    {
        return RedirectToAction("PasswordResetConfirmation", "Account");
    }

    var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
    if (result.Succeeded)
    {
        return RedirectToAction("PasswordResetConfirmation", "Account");
    }

    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(string.Empty, error.Description);
    }

    return View(model);
}

    public async Task<IActionResult> PasswordResetConfirmation()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        List<UserWithRolesViewModel> userWithRoles = new List<UserWithRolesViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            
            userWithRoles.Add(new UserWithRolesViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.ToList().First()
                }
            );
        }

        return View(userWithRoles);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers(string? searchTerm = null)
    {
        // Obter todos os usuários se o campo de buscar não for preenchido se não recuperar
        //de acordo com a consulta

        var users = string.IsNullOrEmpty(searchTerm)
           ? await _userManager.Users.ToListAsync() 
           : await  _userManager.Users
           .Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm))
           .ToListAsync();


        List<UserWithRolesViewModel> usersWithRoles = new List<UserWithRolesViewModel>();
        foreach(var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            roles.ToList();

            var item = new UserWithRolesViewModel
            {
                Name = user.Name,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault()
            };

            usersWithRoles.Add(item);

        }


        return View("Index", usersWithRoles);
    }

    [HttpGet("Update/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Usuário não encontrado.");
        }

        // Mapeia os dados para o ViewModel
        var model = new UpdateUserViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };

        return View(model); // Exibe a página de atualização com os dados do usuário
    }


    [HttpPost("Update/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, UpdateUserViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest("IDs não correspondem.");
        }
        // Localiza o usuário
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Usuário não encontrado.");
        }

        // Atualiza os campos
        if (!string.IsNullOrEmpty(model.Name))
        {
            user.SetName(model.Name);
        }


        if (!string.IsNullOrEmpty(model.Email))
        {
            user.Email = model.Email;
        }


        if (!string.IsNullOrEmpty(model.PhoneNumber))
        {
            user.PhoneNumber = model.PhoneNumber;
        }
        
        // Atualiza no banco
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return RedirectToAction("Index"); 
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID do usuário não pode ser nulo ou vazio.");
        }
        
        var userFind = await _userManager.FindByIdAsync(id);
        if (userFind == null)
        {
            return NotFound($"Usuário não encontrado. {id}");
        }

        var result = await _userManager.DeleteAsync(userFind);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
        }
        else
        {
            TempData["ErrorMessage"] = "Erro ao excluir o usuário.";
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        return RedirectToAction(nameof(Index));
    }


    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddRole()
    {
        var user = await _userManager.Users.ToListAsync();
        if (user == null)
        {
            return BadRequest();
        }

        return View(user);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddRole(string id , string role )
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
        {
            return BadRequest($"Usuário ou role inválidos.");
        }

        // Localiza o usuário pelo ID
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Usuário não encontrado.");
        }

        // Verifica se a role já existe
        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (!roleExists)
        {
            return BadRequest("A role especificada não existe.");
        }
        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return BadRequest("Erro ao remover roles antigas do usuário.");
        }

        // Adiciona a role ao usuário
        var result = await _userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            return BadRequest("Erro ao adicionar a role ao usuário.");
        }

        return RedirectToAction("Index"); // Retorna para a lista de usuários ou outra página
    }

}
