// Program.cs
using CTPATChecklists.Data;
using CTPATChecklists.Middleware;
using CTPATChecklists.Models;
using CTPATChecklists.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddHostedService<LicenciaExpirationService>();

var app = builder.Build();

// 5) Seed de roles, usuarios y configuración global inicial
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // a) Roles y usuarios
    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Superusuario", "Administrador", "Guardia", "Consultor" };
    foreach (var role in roles)
    {
        if (!await roleMgr.RoleExistsAsync(role))
            await roleMgr.CreateAsync(new IdentityRole(role));
    }

    const string suEmail = "admin@ctpat.com";
    const string suPassword = "Admin123!";
    const string adminEmail = "admin@empresa.com";
    const string adminPassword = "Admin456!";

    if (await userMgr.FindByEmailAsync(suEmail) == null)
    {
        var su = new ApplicationUser
        {
            UserName = suEmail,
            Email = suEmail,
            DisplayName = "Super Admin"
        };
        var result = await userMgr.CreateAsync(su, suPassword);
        if (result.Succeeded)
            await userMgr.AddToRoleAsync(su, "Superusuario");
    }

    if (await userMgr.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            DisplayName = "Administrador General"
        };
        var result = await userMgr.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userMgr.AddToRoleAsync(admin, "Administrador");
    }

    // b) Seed de GlobalSettings desde appsettings.json si aún no existe
    var db = services.GetRequiredService<AppDbContext>();
    if (!await db.GlobalSettings.AnyAsync())
    {
        var config = services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("EmailSettings");
        var seedSettings = new GlobalSetting
        {
            SmtpServer = section["SmtpServer"],
            SmtpPort = int.Parse(section["SmtpPort"] ?? "0"),
            SmtpUser = section["SmtpUser"],
            Password = section["Password"],
            FromEmail = section["Email"],
        };
        db.GlobalSettings.Add(seedSettings);
        await db.SaveChangesAsync();
    }
}

// 6) Pipeline de HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<VerificarLicenciaMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
public partial class Program { }

