// Controllers/AccountController.cs
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


namespace CTPATChecklists.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;


        public AccountController(
     SignInManager<ApplicationUser> signInManager,
     UserManager<ApplicationUser> userManager,
     ILogger<AccountController> logger,
     IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // 🔐 Cerrar sesión previa si existe (importante si el usuario actual no tiene licencia válida)
            if (User?.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(); // <-- esto es clave
            }

            var vm = new LoginViewModel { ReturnUrl = returnUrl };
            return View(vm);
        }



        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "No existe un usuario con ese correo.");
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario {Email} inició sesión exitosamente.", vm.Email);

                // 🔄 Refrescar los claims manualmente para asegurar que CompanyId esté presente
                var roles = await _userManager.GetRolesAsync(user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, user.Id ?? "") // 👈 esto es esencial para UserManager.GetUserAsync()

                };

                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

                // ✅ Agregar el claim de CompanyId (importantísimo para aplicar el branding)
                if (!string.IsNullOrEmpty(user.CompanyId))
                    claims.Add(new Claim("CompanyId", user.CompanyId));

                var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
                var principal = new ClaimsPrincipal(identity);

                await _signInManager.SignOutAsync(); // cerrar sesión anterior
                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal); // nueva con claims

                // Redirigir según el rol
                if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                    return Redirect(vm.ReturnUrl);

                if (roles.Contains("Superusuario"))
                    return RedirectToAction("Registrar", "Empresas");

                if (roles.Contains("Administrador"))
                    return RedirectToAction("Index", "Home");

                if (roles.Contains("Guardia") || roles.Contains("Consultor"))
                    return RedirectToAction("Index", "Checklist");

                return RedirectToAction("AccessDenied", "Account");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Usuario {Email} bloqueado.", vm.Email);
                ModelState.AddModelError(string.Empty, "Cuenta bloqueada temporalmente.");
            }
            else if (result.IsNotAllowed)
            {
                _logger.LogWarning("Usuario {Email} no está permitido iniciar sesión.", vm.Email);
                ModelState.AddModelError(string.Empty, "No tienes permiso para iniciar sesión.");
            }
            else if (result.RequiresTwoFactor)
            {
                return RedirectToAction("LoginWith2fa", new { vm.ReturnUrl, vm.RememberMe });
            }
            else
            {
                _logger.LogWarning("Credenciales inválidas para {Email}.", vm.Email);
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            }

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction("ForgotPasswordConfirmation"); // ✅ No revelar si existe o no

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, token }, Request.Scheme);

            var mensajeHtml = $@"
        <h2>Recuperación de contraseña</h2>
        <p>Haz clic en el siguiente botón para restablecer tu contraseña:</p>
        <a href='{callbackUrl}' style='padding: 10px 20px; background-color: #1c87c9; color: white; text-decoration: none; border-radius: 5px;'>Restablecer contraseña</a>
        <p>Si tú no solicitaste esto, ignora este correo.</p>";

            await _emailSender.SendEmailAsync(model.Email, "Restablecer contraseña - CTPAT", mensajeHtml);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return RedirectToAction("ResetPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
                return RedirectToAction("ResetPasswordConfirmation");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

    }
}
