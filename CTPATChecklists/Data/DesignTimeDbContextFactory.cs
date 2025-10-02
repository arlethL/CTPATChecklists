using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CTPATChecklists.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // 1) Directorio raíz del proyecto (donde está el .csproj y appsettings.json)
            var basePath = Directory.GetCurrentDirectory();

            // 2) Carga appsettings.json
            var config = new ConfigurationBuilder()
                             .SetBasePath(basePath)
                             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                             .Build();

            // 3) Toma la cadena de conexión
            var connStr = config.GetConnectionString("DefaultConnection");

            // 4) Construye las opciones de DbContext
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(
                connStr,
                sqlOpts => sqlOpts.EnableRetryOnFailure()
            );

            // 5) Devuelve la instancia del contexto
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
