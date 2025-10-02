using CTPATChecklists.Data;
using CTPATChecklists.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Superusuario,Administrador")]
public class BrandingController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public BrandingController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = User.FindFirst("CompanyId")?.Value;

        var branding = await _db.Brandings
            .FirstOrDefaultAsync(b => b.CompanyId == companyId)
            ?? new Branding { CompanyId = companyId };

        var empresa = await _db.Empresas
            .FirstOrDefaultAsync(e => e.CompanyId == companyId);

        var vm = new BrandingEmpresaViewModel
        {
            Branding = branding,
            CompanyAddress = empresa?.Direccion,
            Phone = empresa?.Telefono,
            ContactEmail = empresa?.Email
        };


        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BrandingEmpresaViewModel vm, IFormFile logoUpload)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var branding = vm.Branding;

        // Subir logo
        if (logoUpload != null && logoUpload.Length > 0)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads", "logos");
            Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(logoUpload.FileName)}";
            var filePath = Path.Combine(uploads, fileName);
            using var stream = System.IO.File.Create(filePath);
            await logoUpload.CopyToAsync(stream);

            branding.LogoPath = $"/uploads/logos/{fileName}";
        }

        // Insertar o actualizar Branding
        var existing = await _db.Brandings
            .FirstOrDefaultAsync(b => b.CompanyId == branding.CompanyId);

        if (existing == null)
        {
            _db.Brandings.Add(branding);
        }
        else
        {
            existing.PrimaryColor = branding.PrimaryColor;
            existing.SecondaryColor = branding.SecondaryColor;
            existing.FontColor = branding.FontColor;
            existing.FontFamily = branding.FontFamily;

            if (!string.IsNullOrEmpty(branding.LogoPath))
                existing.LogoPath = branding.LogoPath;
        }

        // Si es Superusuario, permitir editar datos de empresa
        if (User.IsInRole("Superusuario"))
        {
            var empresa = await _db.Empresas
                .FirstOrDefaultAsync(e => e.CompanyId == branding.CompanyId);

            if (empresa != null)
            {
                empresa.Direccion = vm.CompanyAddress;
                empresa.Telefono = vm.Phone;
                empresa.Email = vm.ContactEmail;

            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "¡Configuración guardada!";
        return RedirectToAction(nameof(Index));
    }
}
