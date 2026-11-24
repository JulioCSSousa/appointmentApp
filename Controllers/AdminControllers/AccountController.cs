using appointmentApp.Models.Entities;
using AppointmentAppMvc.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAppMvc.Controllers
{
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

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedPhoneNumber = UserModel.NormalizePhoneNumber(model.PhoneNumber);
            if (await _userManager.Users.AnyAsync(u => u.PhoneNumberNormalized == normalizedPhoneNumber))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "Este número de telefone já está cadastrado.");
                return View(model);
            }

            var isFirstUser = !await _userManager.Users.AnyAsync();

            var user = new UserModel(model.Name, model.Email, model.PhoneNumber)
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (isFirstUser)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "Client");
                }

                string emailBody = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h1 style='color: #2c3e50;'>Hello, {model.Name}!</h1>
                    <p>Your registration in our <strong>Appointment App</strong> has been completed successfully.</p>
                    <p>You can now access your account using your email: <strong>{model.Email}</strong>.</p>
                    <br>
                    <p>Welcome aboard!</p>
                </div>";

                // Envio em background seguro sem travar a Thread principal
                _ = _emailSender.SendEmailAsync(user.Email, "Welcome to our System!", emailBody);

                TempData["SuccessMessage"] = "Account registered successfully.";
                return RedirectToAction(isFirstUser ? "Login" : "Index");
            }


            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedPhoneNumber = UserModel.NormalizePhoneNumber(model.PhoneNumber);
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumberNormalized == normalizedPhoneNumber);

            var result = user == null
                ? Microsoft.AspNetCore.Identity.SignInResult.Failed
                : await _signInManager.PasswordSignInAsync(
                    user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("IndexAdm", "Home");
                }

                if (await _userManager.IsInRoleAsync(user, "Worker"))
                {
                    return RedirectToAction("IndexWorker", "Home");
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            ViewData["InvalidLogin"] = "Login ou Senha inválidos";
            return View(model);
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
            var user = await _userManager.FindByIdAsync(id);
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

            var user = await _userManager.FindByIdAsync(id);
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

            var user = await _userManager.FindByIdAsync(id);
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

            var user = await _userManager.FindByIdAsync(id);
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
