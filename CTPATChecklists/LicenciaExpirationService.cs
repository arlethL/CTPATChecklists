// Services/LicenciaExpirationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTPATChecklists.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CTPATChecklists.Services
{
    public class LicenciaExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LicenciaExpirationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var thresholds = new Dictionary<int, TimeSpan>
            {
                {365, TimeSpan.FromDays(30)},
                {180, TimeSpan.FromDays(15)},
                {30,  TimeSpan.FromDays(7)},
                {1,   TimeSpan.FromDays(1)}
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    var hoy = DateTime.UtcNow;
                    var pendientes = await db.Licencias
                        .Include(l => l.Empresa)
                        .Where(l => !l.Notificado)
                        .ToListAsync(stoppingToken);

                    foreach (var lic in pendientes)
                    {
                        if (!thresholds.TryGetValue(lic.DiasDuracion, out var umbral))
                            continue;

                        var tiempoRest = lic.FechaExpiracion - hoy;
                        if (tiempoRest <= umbral && tiempoRest > TimeSpan.Zero)
                        {
                            var cfg = await db.GlobalSettings.FirstOrDefaultAsync(stoppingToken);
                            var adminMail = cfg?.FromEmail;

                            try
                            {
                                if (!string.IsNullOrWhiteSpace(adminMail))
                                {
                                    await emailSender.SendEmailAsync(
                                        adminMail,
                                        $"[ALERTA] Licencia {lic.DiasDuracion}d expira pronto",
                                        $"Empresa <strong>{lic.Empresa.Nombre}</strong> vence {lic.FechaExpiracion:dd/MM/yyyy}."
                                    );
                                    Console.WriteLine($"[DEBUG] Notificación enviada a superusuario: {adminMail}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Falló al enviar correo al superusuario ({adminMail}): {ex.Message}");
                            }

                            var contactMail = lic.Empresa.Email;
                            Console.WriteLine($"[DEBUG] Intentando enviar correo a empresa {lic.Empresa.CompanyId} – Email: '{contactMail}'");

                            try
                            {
                                if (!string.IsNullOrWhiteSpace(contactMail))
                                {
                                    await emailSender.SendEmailAsync(
                                        contactMail,
                                        "Tu licencia expirará pronto",
                                        $"Hola <strong>{lic.Empresa.Nombre}</strong>, tu licencia vence el {lic.FechaExpiracion:dd/MM/yyyy}."
                                    );
                                    Console.WriteLine($"[DEBUG] Envío CORRECTO a {contactMail}");
                                }
                                else
                                {
                                    Console.WriteLine($"[WARN] La propiedad Empresa.Email está vacía para {lic.Empresa.CompanyId}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Falló al enviar correo a {contactMail}: {ex.Message}");
                            }

                            lic.Notificado = true;
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // Catch general para evitar que cualquier otro fallo tumbe la app
                    Console.WriteLine($"[FATAL] Error general en LicenciaExpirationService: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
