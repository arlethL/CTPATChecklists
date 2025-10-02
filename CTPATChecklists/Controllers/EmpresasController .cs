using CTPATChecklists.Data;
using CTPATChecklists.Models;
using CTPATChecklists.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CTPATChecklists.Controllers
{
    [Authorize(Roles = "Superusuario")]
    public class EmpresasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public EmpresasController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(EmpresaRegistroViewModel vm)
        {
            // 👇 Evitar validación de CompanyId
            ModelState.Remove("CompanyId");

            if (!ModelState.IsValid)
                return View(vm);

            // 🔐 Generar un ID único
            string generatedCompanyId = Guid.NewGuid().ToString();

            // Paso 1: Crear usuario administrador
            var user = new ApplicationUser
            {
                UserName = vm.AdminEmail,
                Email = vm.AdminEmail,
                CompanyId = generatedCompanyId
            };

            var result = await _userManager.CreateAsync(user, vm.AdminPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Administrador");

            // Paso 2: Crear empresa
            var empresa = new Empresa
            {
                CompanyId = generatedCompanyId,
                Nombre = vm.CompanyName,
                Direccion = vm.CompanyAddress,
                Telefono = vm.Phone,
                Email = vm.Email
            };
            _context.Empresas.Add(empresa);

            // Paso 3: Subir logo (si existe)
            string logoPath = null;
            if (vm.LogoUpload != null && vm.LogoUpload.Length > 0)
            {
                var folder = Path.Combine("wwwroot", "uploads", "logos");
                Directory.CreateDirectory(folder);
                var fileName = $"{generatedCompanyId}_logo{Path.GetExtension(vm.LogoUpload.FileName)}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await vm.LogoUpload.CopyToAsync(stream);

                logoPath = $"/uploads/logos/{fileName}";
            }

            // Paso 4: Guardar branding
            var branding = new Branding
            {
                CompanyId = generatedCompanyId,
                CompanyName = vm.CompanyName,
                PrimaryColor = vm.PrimaryColor,
                SecondaryColor = vm.SecondaryColor,
                FontColor = vm.FontColor,
                FontFamily = vm.FontFamily,
                LogoPath = logoPath
            };
            _context.Brandings.Add(branding);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Empresa registrada correctamente.";
            return RedirectToAction("Registrar");
        }
    }
}
