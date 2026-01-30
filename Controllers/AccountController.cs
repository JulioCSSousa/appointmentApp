using appointmentApp.Models.Entities;
using AppointmentAppMvc.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sprache;

public class AccountController : Controller
{
    private readonly UserManager<UserModel> _userManager;
    private readonly SignInManager<UserModel> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailSender _emailSender;

    public AccountController(UserManager<UserModel> userManager,
        SignInManager<UserModel> signInManager,
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
                var user = new UserModel(model.Name, model.Email, model.PhoneNumber)
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
                var user = new UserModel(model.Name, model.Email, model.PhoneNumber)
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
                if (user == null)
                {
                    return BadRequest();
                }
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.ToList();

                if (roles == null)
                {
                    return BadRequest("Role nao encontrado");
                }

                if (roles.FirstOrDefault("Admin") == "Admin")
                {
                    return RedirectToAction("IndexAdm", "Home");

                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewData["UnauthorizedMessage"] = "Invalid Login";
            ModelState.AddModelError("", "Login invalido.");
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
            // N�o expor se o e-mail est� correto ou n�o por motivos de seguran�a
            return RedirectToAction("Login", "Account");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action(
            "PasswordReset",
            "Account",
            new { token = token, email = user.Email },
            protocol: Request.Scheme);

        // Envia o link de recupera��o de senha para o e-mail do usu�rio
        await _emailSender.SendEmailAsync(
            email,
            "Redefini��o de Senha",
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
            return BadRequest("Token inv�lido.");
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
    public async Task<IActionResult> Index(string? searchTerm = null, int page = 1)
    {
        int itemByPge = 10;
        int skip = (page - 1) * itemByPge;

        // 1. Criamos a QUERY (IQueryable), não a lista ainda
        var query = _userManager.Users.AsQueryable();

        // 2. Aplicamos o filtro se existir
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm));
        }

        // 3. CONTAMOS o total de registros que batem com o filtro (antes de paginar!)
        // Isso é o que faz a sua ViewBag.TotalPages funcionar
        int totalItems = await query.CountAsync();

        // 4. Agora sim, paginamos e transformamos em Lista
        var users = await query
            .OrderBy(u => u.Name)
            .Skip(skip)
            .Take(itemByPge)
            .ToListAsync();

        // 5. Preenchemos as ViewBags com base no total REAL do banco
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / itemByPge);
        ViewBag.SearchTerm = searchTerm;

        // 6. Mapeamento para o ViewModel (seu loop está correto)
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

        return View("Index", usersWithRoles);
    }

    [HttpGet("Update/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Usuario nao encontrado.");
        }

        // Mapeia os dados para o ViewModel
        var model = new UpdateUserViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.UserName,
            PhoneNumber = user.PhoneNumber
        };

        return View(model); // Exibe a p�gina de atualiza��o com os dados do usu�rio
    }


    [HttpPost("Update/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, UpdateUserViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest("IDs n�o correspondem.");
        }
        // Localiza o usu�rio
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Usu�rio n�o encontrado.");
        }

        // Atualiza os campos
        if (!string.IsNullOrEmpty(model.Name))
        {
            user.SetName(model.Name);
        }


        if (!string.IsNullOrEmpty(model.Email))
        {

            user.UserName = model.Email;
            user.NormalizedUserName = model.Email.ToUpper();
            user.Email = model.Email;
            user.NormalizedEmail = model.Email.ToUpper();

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
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID do usu�rio n�o pode ser nulo ou vazio.");
        }

        var userFind = await _userManager.FindByIdAsync(id);
        if (userFind == null)
        {
            return NotFound($"Usu�rio n�o encontrado. {id}");
        }

        var result = await _userManager.DeleteAsync(userFind);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Usu�rio exclu�do com sucesso!";
        }
        else
        {
            TempData["ErrorMessage"] = "Erro ao excluir o usu�rio.";
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
    public async Task<IActionResult> AddRole(string id, string role)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
        {
            return BadRequest($"Usu�rio ou role inv�lidos.");
        }

        // Localiza o usu�rio pelo ID
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Usu�rio n�o encontrado.");
        }

        // Verifica se a role j� existe
        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (!roleExists)
        {
            return BadRequest("A role especificada n�o existe.");
        }
        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return BadRequest("Erro ao remover roles antigas do usu�rio.");
        }

        // Adiciona a role ao usu�rio
        var result = await _userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            return BadRequest("Erro ao adicionar a role ao usu�rio.");
        }

        return RedirectToAction("Index"); // Retorna para a lista de usu�rios ou outra p�gina
    }

}
