// Data/AppDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CTPATChecklists.Models;
using System.ComponentModel;

namespace CTPATChecklists.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Checklist> Checklists { get; set; }
        public DbSet<PuntoChecklist> PuntosChecklist { get; set; }
        public DbSet<FotoChecklist> FotosChecklist { get; set; }

        public DbSet<Branding> Brandings { get; set; }

        public DbSet<Empresa> Empresas { get; set; }

        public DbSet<Licencia> Licencias { get; set; }
        public DbSet<Camara> Camaras { get; set; }

        public DbSet<GlobalSetting> GlobalSettings { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Primero, invoca el mapeo de Identity
            base.OnModelCreating(modelBuilder);

            // Configuración de relaciones para Checklist
            modelBuilder.Entity<Checklist>()
                .HasMany(c => c.Puntos)
                .WithOne(p => p.Checklist)
                .HasForeignKey(p => p.ChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Checklist>()
                .HasMany(c => c.Fotos)
                .WithOne(f => f.Checklist)
                .HasForeignKey(f => f.ChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            // Aquí puedes agregar más configuraciones si es necesario
        }
    }
}