using CTPATChecklists.Data;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CTPATChecklists.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly AppDbContext _db;

        public EmailSender(AppDbContext db)
        {
            _db = db;
        }

        public async Task SendEmailAsync(string emailDestino, string subject, string htmlMessage)
        {
            // Recupera la única fila de configuración global
            var cfg = await _db.GlobalSettings.FirstOrDefaultAsync();
            if (cfg == null)
                throw new InvalidOperationException("No hay configuración de correo en la base de datos.");

            using var smtpClient = new SmtpClient(cfg.SmtpServer, cfg.SmtpPort)
            {
                Credentials = new NetworkCredential(cfg.SmtpUser, cfg.Password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(cfg.FromEmail, "CTPAT Checklists"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(emailDestino);

            await smtpClient.SendMailAsync(message);
        }
    }
}
