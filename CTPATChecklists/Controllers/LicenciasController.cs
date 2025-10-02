using CTPATChecklists.Data;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Superusuario")]
public class LicenciasController : Controller
{
    private readonly AppDbContext _db;

    public LicenciasController(AppDbContext db)
    {
        _db = db;
    }

    private Dictionary<string, ConteoLicenciasDto> ObtenerConteoLicencias()
    {
        return _db.Empresas.ToDictionary(
            e => e.CompanyId,
            e => new ConteoLicenciasDto
            {
                UnoDia = _db.Licencias.Count(l =>
                    l.CompanyId == e.CompanyId &&
                    l.DiasDuracion == 1 &&
                    l.FueCanjeada),
                UnoMes = _db.Licencias.Count(l =>
                    l.CompanyId == e.CompanyId &&
                    l.DiasDuracion == 30 &&
                    l.FueCanjeada),
                UnoAno = _db.Licencias.Count(l =>
                    l.CompanyId == e.CompanyId &&
                    l.DiasDuracion == 365 &&
                    l.FueCanjeada)
            });
    }


    public IActionResult Generar()
    {
        ViewBag.Empresas = _db.Empresas.ToList();
        ViewBag.ConteosLicencias = ObtenerConteoLicencias();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Generar(string companyId, string tipo, int cantidad)
    {
        ViewBag.Empresas = _db.Empresas.ToList();
        ViewBag.ConteosLicencias = ObtenerConteoLicencias();

        int diasDuracion = tipo switch
        {
            "dias" => 1,
            "meses" => 30,
            "años" => 365,
            _ => -1
        };

        if (string.IsNullOrWhiteSpace(companyId) || diasDuracion == -1 || cantidad != 1)
        {
            ViewBag.Error = "Solo puedes generar licencias de duración fija (1 día, 1 mes o 1 año).";
            return View();
        }

        // Limitar a 3 canjes (activaciones) para 1 día y 1 mes
        if (diasDuracion == 1 || diasDuracion == 30)
        {
            int canjeadasTipo = await _db.Licencias.CountAsync(l =>
                l.CompanyId == companyId &&
                l.DiasDuracion == diasDuracion &&
                l.FueCanjeada);

            if (canjeadasTipo >= 3)
            {
                string tipoTexto = diasDuracion == 1 ? "1 día" : "1 mes";
                ViewBag.Error = $"⚠️ Esta empresa ya ha canjeado {canjeadasTipo} licencias de tipo {tipoTexto}. Solo se permiten 3.";
                return View();
            }
        }


        DateTime inicio = DateTime.UtcNow;
        DateTime fin = inicio.AddDays(diasDuracion);
        string codigo = Guid.NewGuid().ToString("N")[..10].ToUpper();

        var licencia = new Licencia
        {
            CompanyId = companyId,
            CodigoActivacion = codigo,
            FechaInicio = inicio,
            FechaExpiracion = fin,
            DiasDuracion = diasDuracion,
            Activa = false
        };

        _db.Licencias.Add(licencia);
        await _db.SaveChangesAsync();

        ViewBag.Mensaje = $"✅ Código generado: <strong>{codigo}</strong> válido hasta <strong>{fin.ToLocalTime():dd/MM/yyyy HH:mm}</strong>";
        ViewBag.ConteosLicencias = ObtenerConteoLicencias();
        return View();
    }

    public async Task<IActionResult> Activas()
    {
        var licencias = await _db.Licencias
            .Include(l => l.Empresa)
            .Where(l => l.Activa)
            .OrderByDescending(l => l.FechaInicio)
            .ToListAsync();

        return View(licencias);
    }
}
