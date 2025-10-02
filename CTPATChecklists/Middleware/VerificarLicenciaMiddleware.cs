using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CTPATChecklists.Data;

namespace CTPATChecklists.Middleware
{
    public class VerificarLicenciaMiddleware
    {
        private readonly RequestDelegate _next;

        public VerificarLicenciaMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            var path = context.Request.Path.Value?.TrimEnd('/').ToLower();

            var rutasIgnoradas = new[]
            {
                "/account/login",
                "/account/logout",
                "/licencia/expirada",
                "/licenciasempresa/canjearcodigo",
                "/configuracion/agregar",
                "/css", "/js", "/lib", "/favicon", "/imagenes"
            };

            if (rutasIgnoradas.Any(r => path.StartsWith(r)))
            {
                await _next(context);
                return;
            }

            var user = context.User;

            if (!user.Identity.IsAuthenticated || user.IsInRole("Superusuario"))
            {
                await _next(context);
                return;
            }

            var companyId = user.Claims.FirstOrDefault(c => c.Type == "CompanyId")?.Value;

            if (!string.IsNullOrEmpty(companyId))
            {
                var licencia = await db.Licencias
                    .Where(l => l.CompanyId == companyId && l.Activa)
                    .OrderByDescending(l => l.FechaExpiracion)
                    .FirstOrDefaultAsync();

                if (licencia == null || licencia.FechaExpiracion <= DateTime.UtcNow)
                {
                    var isPost = context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase);
                    var excepcionesPost = new[] { "/configuracion/agregar", "/licenciasempresa/canjearcodigo" };
                    var esRutaPermitida = excepcionesPost.Contains(path);

                    if (!isPost || !esRutaPermitida)
                    {
                        if (!isPost)
                            context.Response.Redirect("/Licencia/Expirada");
                        else
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Tu licencia ha expirado.");
                        }
                        return;
                    }
                }

                var tiempoRestante = licencia.FechaExpiracion - DateTime.UtcNow;
                var duracion = licencia.FechaExpiracion - licencia.FechaInicio;

                double diasRestantes = tiempoRestante.TotalDays;
                int horasRestantes = (int)tiempoRestante.TotalHours;

                bool debeNotificar = false;

                if (duracion.TotalDays >= 360 && diasRestantes <= 30)
                    debeNotificar = true;
                else if (duracion.TotalDays >= 180 && diasRestantes <= 15)
                    debeNotificar = true;
                else if (duracion.TotalDays >= 30 && diasRestantes <= 7)
                    debeNotificar = true;
                else if (duracion.TotalDays <= 1 && tiempoRestante.TotalHours <= 24)
                    debeNotificar = true;

                if (debeNotificar)
                {
                    context.Items["AvisoLicencia"] = $"⚠️ Tu licencia expirará en {Math.Floor(diasRestantes)} días y {horasRestantes % 24} horas. Contacta al administrador si deseas renovarla.";

                    if (!licencia.Notificado)
                    {
                        try
                        {
                            // Obtener configuración global desde base de datos
                            var cfg = await db.GlobalSettings.FirstOrDefaultAsync();
                            if (cfg == null)
                                return;

                            var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.CompanyId == companyId);
                            string nombreEmpresa = empresa?.Nombre ?? "(Empresa sin nombre)";

                            var smtpClient = new SmtpClient(cfg.SmtpServer)
                            {
                                Port = cfg.SmtpPort,
                                Credentials = new NetworkCredential(cfg.SmtpUser, cfg.Password),
                                EnableSsl = true
                            };

                            var mail = new MailMessage
                            {
                                From = new MailAddress(cfg.FromEmail, "CTPAT Checklists"),
                                Subject = "⚠️ Licencia próxima a expirar",
                                Body = $@"<p>La licencia de la empresa <strong>{nombreEmpresa}</strong> está próxima a expirar.</p>
                                         <p>Expira: <strong>{licencia.FechaExpiracion:dd/MM/yyyy HH:mm}</strong></p>
                                         <p>Tiempo restante: <strong>{Math.Floor(diasRestantes)} días y {horasRestantes % 24} horas</strong></p>",
                                IsBodyHtml = true
                            };

                            // Enviar al superusuario
                            mail.To.Add(cfg.FromEmail);

                            // Enviar también al correo de la empresa si está disponible
                            if (!string.IsNullOrWhiteSpace(empresa?.Email))
                            {
                                mail.To.Add(empresa.Email);
                            }

                            await smtpClient.SendMailAsync(mail);

                            licencia.Notificado = true;
                            db.Licencias.Update(licencia);
                            await db.SaveChangesAsync();
                        }
                        catch
                        {
                            // Puedes loguear errores aquí si deseas
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
