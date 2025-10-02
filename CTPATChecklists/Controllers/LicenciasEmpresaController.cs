using CTPATChecklists.Data;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CTPATChecklists.Controllers
{
    [Authorize]
    public class LicenciasEmpresaController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LicenciasEmpresaController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // 🔒 Canje después de que la licencia haya expirado (vista Expirada.cshtml)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CanjearCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                ViewBag.ErrorCodigo = "Debes ingresar un código válido.";
                return View("~/Views/Licencia/Expirada.cshtml");
            }

            var user = await _userManager.GetUserAsync(User);
            var empresaId = user?.CompanyId;

            if (string.IsNullOrEmpty(empresaId))
            {
                ViewBag.ErrorCodigo = "No se pudo determinar la empresa del usuario.";
                return View("~/Views/Licencia/Expirada.cshtml");
            }

            var licencia = await _db.Licencias
                .FirstOrDefaultAsync(l => l.CompanyId == empresaId && l.CodigoActivacion == codigo && !l.Activa);

            if (licencia == null)
            {
                ViewBag.ErrorCodigo = "El código es inválido, ya fue usado o no pertenece a tu empresa.";
                return View("~/Views/Licencia/Expirada.cshtml");
            }

            licencia.Activa = true;
            licencia.FueCanjeada = true;
            licencia.FechaInicio = DateTime.UtcNow;
            licencia.FechaExpiracion = licencia.FechaInicio.AddDays(licencia.DiasDuracion); // ✅ usar DiasDuracion
            licencia.Notificado = false;

            _db.Licencias.Update(licencia);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // 🆕 GET: Mostrar formulario de canje anticipado
        [Authorize(Roles = "Administrador")]
        public IActionResult CanjearDesdeAdmin()
        {
            return View();
        }

        // 🆕 POST: Procesar canje anticipado desde administrador
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CanjearDesdeAdmin(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Error"] = "Ingresa un código válido.";
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            var empresaId = user?.CompanyId;

            if (string.IsNullOrEmpty(empresaId))
            {
                TempData["Error"] = "No se pudo identificar tu empresa.";
                return View();
            }

            var nuevaLicencia = await _db.Licencias
                .FirstOrDefaultAsync(l => l.CodigoActivacion == codigo && !l.Activa && l.CompanyId == empresaId);

            if (nuevaLicencia == null)
            {
                TempData["Error"] = "El código ingresado es inválido, ya fue usado o no pertenece a tu empresa.";
                return View();
            }

            // Desactivar la licencia actual si aún está vigente
            var licenciaActual = await _db.Licencias
                .Where(l => l.CompanyId == empresaId && l.Activa)
                .OrderByDescending(l => l.FechaExpiracion)
                .FirstOrDefaultAsync();

            TimeSpan tiempoRestante = TimeSpan.Zero;

            if (licenciaActual != null && licenciaActual.FechaExpiracion > DateTime.UtcNow)
            {
                tiempoRestante = licenciaActual.FechaExpiracion - DateTime.UtcNow;
                licenciaActual.Activa = false;
                _db.Licencias.Update(licenciaActual);
            }

            // ✅ Activar la nueva licencia y sumar tiempo restante si aplica
            nuevaLicencia.Activa = true;
            nuevaLicencia.FueCanjeada=true;
            nuevaLicencia.FechaInicio = DateTime.UtcNow;
            nuevaLicencia.FechaExpiracion = nuevaLicencia.FechaInicio
                .AddDays(nuevaLicencia.DiasDuracion)
                .Add(tiempoRestante);

            nuevaLicencia.Notificado = false;

            _db.Licencias.Update(nuevaLicencia);
            await _db.SaveChangesAsync();

            TempData["Exito"] = $"Licencia activada correctamente. Nueva fecha de expiración: {nuevaLicencia.FechaExpiracion:dd/MM/yyyy HH:mm}.";
            return View();
        }
    }
}