using CTPATChecklists.Data;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CTPATChecklists.Controllers
{
    [Authorize(Roles = "Superusuario")]
    public class GlobalSettingsController : Controller
    {
        private readonly AppDbContext _db;
        public GlobalSettingsController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var cfg = await _db.GlobalSettings.FirstOrDefaultAsync() ?? new GlobalSetting();

            // 🔔 Verifica si están usando valores genéricos
            bool esGenerica = cfg.SmtpServer == "smtp.ejemplo.com" || cfg.FromEmail == "noreply@ejemplo.com";
            ViewBag.EsGenerica = esGenerica;

            return View(cfg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(GlobalSetting model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _db.GlobalSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _db.GlobalSettings.Add(model);
            }
            else
            {
                existing.SmtpServer = model.SmtpServer;
                existing.SmtpPort = model.SmtpPort;
                existing.SmtpUser = model.SmtpUser;
                existing.Password = model.Password;
                existing.FromEmail = model.FromEmail;
            }
            await _db.SaveChangesAsync();

            ViewBag.Mensaje = "Configuración guardada correctamente.";

            // 🔁 Recalcular la bandera después de guardar
            ViewBag.EsGenerica = model.SmtpServer == "smtp.ejemplo.com" || model.FromEmail == "noreply@ejemplo.com";

            return View(model);
        }
    }
}
