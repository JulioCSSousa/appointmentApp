using appointmentApp.Models.Entities;
using AppointmentAppMvc.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentAppMvc.Services.Stores;
using appointmentApp.Constants;

namespace AppointmentAppMvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly IStoreContext _storeContext;

        private async Task<StoreModel?> GetCurrentStoreAsync() =>
            await _storeContext.GetCurrentStoreAsync();

        private bool IsSuperAdmin() => User.IsInRole(Roles.SuperAdmin);

        private async Task<UserModel?> FindTenantUserAsync(string id)
        {
            if (IsSuperAdmin())
            {
                return await _userManager.FindByIdAsync(id);
            }

            var store = await GetCurrentStoreAsync();
            return store == null
                ? null
                : await _userManager.Users
                    .SingleOrDefaultAsync(user => user.Id == id && user.StoreId == store.Id);
        }

        public AccountController(UserManager<UserModel> userManager,
            SignInManager<UserModel> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            IStoreContext storeContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _storeContext = storeContext;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var store = await GetCurrentStoreAsync();
            if (store == null)
            {
                return NotFound("Loja não encontrada.");
            }

            ViewData["StoreSlug"] = store.Slug;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var store = await GetCurrentStoreAsync();
            if (store == null)
            {
                return NotFound("Loja não encontrada.");
            }

            ViewData["StoreSlug"] = store.Slug;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedPhoneNumber = UserModel.NormalizePhoneNumber(model.PhoneNumber);

            // 1. Verifica telefone duplicado APENAS na loja atual
            if (await _userManager.Users.AnyAsync(u => u.StoreId == store.Id && u.PhoneNumberNormalized == normalizedPhoneNumber))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "Este número de telefone já está cadastrado nesta loja.");
                return View(model);
            }

            // 2. IgnoreQueryFilters garante que só o primeiro usuário do sistema inteiro vire Admin
            var isFirstUser = !await _userManager.Users.IgnoreQueryFilters().AnyAsync();

            var user = new UserModel(model.Name, model.Email, model.PhoneNumber)
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            user.SetStore(store.Id);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, isFirstUser ? "Admin" : "Client");

                string emailBody = $@"
        <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
            <h1 style='color: #2c3e50;'>Hello, {model.Name}!</h1>
            <p>Your registration in our <strong>Appointment App</strong> has been completed successfully.</p>
            <p>You can now access your account using your email: <strong>{model.Email}</strong>.</p>
            <br>
            <p>Welcome aboard!</p>
        </div>";

                _ = _emailSender.SendEmailAsync(user.Email, "Welcome to our System!", emailBody);

                TempData["SuccessMessage"] = "Account registered successfully.";

                // 3. Se for Admin, vai para /Account/AdminLogin (sem slug). Se for Cliente, mantém o slug da loja.
                return isFirstUser
                    ? RedirectToAction(nameof(AdminLogin))
                    : RedirectToAction(nameof(ClientLogin), new { storeSlug = store.Slug });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => RedirectToAction(nameof(AdminLogin));

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult AdminLogin() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(EmailLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("AdminLogin", model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null ||
                (!await _userManager.IsInRoleAsync(user, Roles.Admin) &&
                 !await _userManager.IsInRoleAsync(user, Roles.SuperAdmin)))
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View("AdminLogin", model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
                {
                    return RedirectToAction("Index", "SuperAdmin");
                }

                return RedirectToAction("IndexAdm", "Home");
            }

            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View("AdminLogin", model);
        }

        [HttpGet]
        public IActionResult ProfessionalLogin()
        {
            ViewData["LoginTitle"] = "Login do profissional";
            ViewData["LoginAction"] = nameof(ProfessionalLogin);
            return View("PhoneLogin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfessionalLogin(LoginViewModel model)
        {
            return await PhoneLoginAsync(model, "Worker", "IndexWorker");
        }

        [HttpGet]
        public async Task<IActionResult> ClientLogin(string? returnUrl = null)
        {
            var store = await GetCurrentStoreAsync();
            if (store == null)
            {
                return NotFound("Loja não encontrada.");
            }

            ViewData["StoreSlug"] = store.Slug;
            ViewData["LoginTitle"] = "Login do cliente";
            ViewData["LoginAction"] = nameof(ClientLogin);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClientLogin(LoginViewModel model)
        {
            return await PhoneLoginAsync(model, "Client", "IndexClient");
        }

        private async Task<IActionResult> PhoneLoginAsync(LoginViewModel model, string role, string destinationAction)
        {
            var store = await GetCurrentStoreAsync();

            if (!ModelState.IsValid)
            {
                ViewData["StoreSlug"] = store?.Slug;
                ViewData["LoginTitle"] = role == "Worker" ? "Login do profissional" : "Login do cliente";
                ViewData["LoginAction"] = role == "Worker" ? nameof(ProfessionalLogin) : nameof(ClientLogin);
                return View(role == "Client" ? "ClientLogin" : "PhoneLogin", model);
            }

            var normalizedPhoneNumber = UserModel.NormalizePhoneNumber(model.PhoneNumber);

            // Garante que está buscando o usuário dentro da loja atual (se houver loja na rota)
            var userQuery = _userManager.Users.Where(u => u.PhoneNumberNormalized == normalizedPhoneNumber);
            if (store != null)
            {
                userQuery = userQuery.Where(u => u.StoreId == store.Id);
            }

            var user = await userQuery.FirstOrDefaultAsync();

            var result = user == null || !await _userManager.IsInRoleAsync(user, role)
                ? Microsoft.AspNetCore.Identity.SignInResult.Failed
                : await _signInManager.PasswordSignInAsync(
                    user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (role == "Client")
                {
                    if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    // Redireciona usando a rota "store": /loja/{storeSlug}/Home/IndexClient
                    return RedirectToAction(destinationAction, "Home", new { storeSlug = store?.Slug });
                }

                // Profissional (Worker) vai para a área interna sem slug
                return RedirectToAction(destinationAction, "Home");
            }

            ModelState.AddModelError(string.Empty, "Telefone ou senha inválidos.");
            ViewData["StoreSlug"] = store?.Slug;
            ViewData["LoginTitle"] = role == "Worker" ? "Login do profissional" : "Login do cliente";
            ViewData["LoginAction"] = role == "Worker" ? nameof(ProfessionalLogin) : nameof(ClientLogin);
            return View(role == "Client" ? "ClientLogin" : "PhoneLogin", model);
        }

        [HttpGet]
        public IActionResult PasswordRecovery() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordRecovery([FromForm] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Proteção contra enumeração de e-mails
                return RedirectToAction("PasswordRecoveryConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                "PasswordReset",
                "Account",
                new { token = token, email = user.Email },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "Password Reset Request",
                $"Please reset your password by clicking here: <a href='{callbackUrl}'>Reset Password</a>");

            return RedirectToAction("PasswordRecoveryConfirmation");
        }

        [HttpGet]
        public IActionResult PasswordRecoveryConfirmation() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult PasswordReset(string email, string token)
        {
            if (token == null || email == null)
            {
                TempData["ErrorMessage"] = "Invalid password reset token.";
                return RedirectToAction("Login");
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
                return RedirectToAction("PasswordResetConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("PasswordResetConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult PasswordResetConfirmation() => View();

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? searchTerm = null, int page = 1)
        {
            int itemsPerPage = 10;
            int skip = (page - 1) * itemsPerPage;

            var query = _userManager.Users;
            if (!IsSuperAdmin())
            {
                var store = await GetCurrentStoreAsync();
                if (store == null)
                {
                    return NotFound("Loja não encontrada.");
                }

                query = query.Where(user => user.StoreId == store.Id);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            int totalItems = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Name)
                .Skip(skip)
                .Take(itemsPerPage)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            ViewBag.SearchTerm = searchTerm;

            var usersWithRoles = new List<UserWithRolesViewModel>();
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
                    Role = roles.FirstOrDefault() ?? "No Role Assigned"
                });
            }

            return View("Index", usersWithRoles);
        }

        [HttpGet("Update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string id)
        {
            var user = await FindTenantUserAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            var model = new UpdateUserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost("Update/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string id, UpdateUserViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID mismatch.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await FindTenantUserAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(model.Name)) user.SetName(model.Name);
            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var normalizedPhoneNumber = UserModel.NormalizePhoneNumber(model.PhoneNumber);
                var phoneAlreadyUsed = await _userManager.Users
                    .AnyAsync(u => u.Id != user.Id && u.PhoneNumberNormalized == normalizedPhoneNumber);

                if (phoneAlreadyUsed)
                {
                    ModelState.AddModelError(nameof(model.PhoneNumber), "Este número de telefone já está cadastrado.");
                    return View(model);
                }

                user.SetPhoneNumber(model.PhoneNumber);
            }

            if (!string.IsNullOrEmpty(model.Email) && !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = model.Email;
                user.NormalizedUserName = model.Email.ToUpper();
                user.Email = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "User ID cannot be null.";
                return RedirectToAction("Index");
            }

            var user = await FindTenantUserAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddRole(string? searchTerm = null, int page = 1)
        {
            int itemsPerPage = 10;
            int skip = (page - 1) * itemsPerPage;

            var query = _userManager.Users;
            if (!IsSuperAdmin())
            {
                var store = await GetCurrentStoreAsync();
                if (store == null)
                {
                    return NotFound("Loja não encontrada.");
                }

                query = query.Where(user => user.StoreId == store.Id);
            }
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            int totalItems = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Name)
                .Skip(skip)
                .Take(itemsPerPage)
                .ToListAsync();

            var userWithRoles = new List<UserWithRolesViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userWithRoles.Add(new UserWithRolesViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = u.Name,
                    PhoneNumber = u.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? "No Role Assigned"
                });
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            ViewBag.SearchTerm = searchTerm;

            return View(userWithRoles);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRole(string id, string role)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "User ID or Role cannot be empty.";
                return RedirectToAction("Index");
            }

            var user = await FindTenantUserAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (!roleExists)
            {
                TempData["ErrorMessage"] = "The specified role does not exist.";
                return RedirectToAction("Index");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to clear previous user roles.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to update user role.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "User role updated successfully.";
            return RedirectToAction("Index");
        }
    
    // POST: /Account/Logout
    [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

    }
}
