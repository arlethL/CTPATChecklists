using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CTPATChecklists.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

[Authorize(Roles = "Superusuario,Administrador")]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsuariosController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Create");
    }

    public IActionResult Create()
    {
        var availableRoles = ObtenerRolesDisponiblesParaUsuario();
        var model = new CreateUserViewModel
        {
            AvailableRoles = availableRoles
        };

        return View("~/Views/usuarios/create.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        model.AvailableRoles = ObtenerRolesDisponiblesParaUsuario();

        ModelState.Remove(nameof(model.CompanyId));
        ModelState.Remove(nameof(model.CompanyName));

        if (!ModelState.IsValid)
        {
            return View("~/Views/usuarios/create.cshtml", model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError("", "Las contraseñas no coinciden.");
            return View("~/Views/usuarios/create.cshtml", model);
        }

        if (!model.AvailableRoles.Contains(model.SelectedRole))
        {
            ModelState.AddModelError("", "No tienes permiso para asignar este rol.");
            return View("~/Views/usuarios/create.cshtml", model);
        }

        if (model.SelectedRole == "Administrador" && !User.IsInRole("Superusuario"))
        {
            ModelState.AddModelError("", "Solo el Superusuario puede crear otros Administradores.");
            return View("~/Views/usuarios/create.cshtml", model);
        }

        var existente = await _userManager.FindByEmailAsync(model.Email);
        if (existente != null)
        {
            ModelState.AddModelError("", "Ya existe un usuario con este correo.");
            return View("~/Views/usuarios/create.cshtml", model);
        }

        // ✅ Asignar correctamente el CompanyId según quién lo crea
        string companyId;
        if (User.IsInRole("Superusuario"))
        {
            // Si lo crea un superusuario y especifica un CompanyId, lo usa
            companyId = string.IsNullOrEmpty(model.CompanyId)
                ? Guid.NewGuid().ToString()
                : model.CompanyId;
        }
        else
        {
            // Si lo crea un admin, debe heredar el CompanyId del creador
            var creator = await _userManager.GetUserAsync(User);
            companyId = creator?.CompanyId;

            if (string.IsNullOrEmpty(companyId))
            {
                ModelState.AddModelError("", "Error al determinar el CompanyId del administrador actual.");
                return View("~/Views/usuarios/create.cshtml", model);
            }
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.Email,
            CompanyId = companyId
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.SelectedRole);

            // ✅ Añadir Claim para branding
            await _userManager.AddClaimAsync(user, new Claim("CompanyId", companyId));

            TempData["Success"] = "✅ Usuario creado correctamente.";
            return RedirectToAction("Create");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        TempData["Success"] = null;
        return View("~/Views/usuarios/create.cshtml", model);
    }

    private List<string> ObtenerRolesDisponiblesParaUsuario()
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();

        if (User.IsInRole("Superusuario"))
            return roles;

        return roles.Where(r => r == "Guardia" || r == "Consultor").ToList();
    }
}
