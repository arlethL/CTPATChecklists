using CTPATChecklists.Data;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CTPATChecklists.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ConfiguracionController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfiguracionController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);
            var companyId = usuario?.CompanyId;

            if (string.IsNullOrEmpty(companyId))
            {
                return View(new List<Camara>());
            }

            var camaras = _db.Camaras
                             .Where(c => c.EmpresaId == companyId)
                             .ToList();

            return View(camaras);
        }


        [HttpGet]
        public IActionResult Agregar()
        {
            return View(new Camara()); // ⬅ para que el model no sea null
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(Camara camara)
        {
            try
            {
                Debug.WriteLine("🟢 Entró al POST Agregar");

                if (!User.Identity.IsAuthenticated)
                {
                    Debug.WriteLine("🔒 El usuario NO está autenticado");
                    return Content("🔒 Usuario no autenticado");
                }

                var companyClaim = User.FindFirst("CompanyId");
                if (companyClaim == null)
                {
                    Debug.WriteLine("❌ No se encontró el claim CompanyId");
                    return Content("❌ Claim CompanyId no encontrado");
                }

                var companyId = companyClaim.Value;
                Debug.WriteLine("✅ CompanyId: " + companyId);

                camara.EmpresaId = companyId;

                _db.Camaras.Add(camara);
                await _db.SaveChangesAsync();

                Debug.WriteLine("✅ Cámara guardada");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("🔥 ERROR: " + ex.Message);
                return Content("🔥 ERROR: " + ex.Message);
            }

        }


        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var camara = await _db.Camaras.FindAsync(id);
            if (camara == null) return NotFound();
            return View(camara);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Camara camara)
        {
            if (!ModelState.IsValid) return View(camara);
            _db.Camaras.Update(camara);
            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Cámara actualizada correctamente.";
            return RedirectToAction("Index");


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var camara = await _db.Camaras.FindAsync(id);
            if (camara == null) return NotFound();
            _db.Camaras.Remove(camara);
            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Cámara eliminada correctamente.";
            return RedirectToAction("Index");
        }

    }

}