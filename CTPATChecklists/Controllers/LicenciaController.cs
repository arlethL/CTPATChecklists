using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CTPATChecklists.Controllers
{
    public class LicenciaController : Controller
    {
        [AllowAnonymous]
        public IActionResult Expirada()
        {
            // Pasar mensaje de error si viene desde el controlador de canjeo
            ViewBag.ErrorCodigo = TempData["ErrorCodigo"];
            return View();
        }
    }
}
