using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace CTPATChecklists.Controllers
{
    public class CorreoTestController : Controller
    {
        private readonly IEmailSender _emailSender;

        public CorreoTestController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task<IActionResult> EnviarCorreoPrueba()
        {
            try
            {
                await _emailSender.SendEmailAsync(
                    "arlethleon735@gmail.com", // cambia esto por un correo válido que controles
                    "📧 Prueba desde CTPATChecklist",
                    "<h3>✅ Este es un correo de prueba enviado correctamente desde tu aplicación.</h3><p>¡Ya estás listo para recuperar contraseñas!</p>"
                );

                return Content("✅ El correo fue enviado correctamente.");
            }
            catch (System.Exception ex)
            {
                return Content("❌ Error al enviar el correo: " + ex.Message);
            }
        }
    }
}
