using CTPATChecklists.Data;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace CTPATChecklists.Controllers
{
    [Authorize(Roles = "Guardia,Administrador,Superusuario,Consultor")]
    public class ChecklistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] Descripciones17 = new[]
        {
            "Defensas/Llantas/Rines",
            "Motor",
            "Llantas",
            "Piso (Tractor)",
            "Tanques de combustible",
            "Compartimentos de cabina/dormitorio",
            "Tanques de aire",
            "Flechas de transmisión",
            "Quinta rueda",
            "Parte inferior/exterior",
            "Puertas internas/externas",
            "Piso (Remolque)",
            "Paredes laterales",
            "Pared frontal",
            "Techo",
            "Unidad de refrigeración",
            "Escape"
        };

        public ChecklistController(
            AppDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.Checklists
                                .Include(c => c.Usuario)
                                .OrderByDescending(c => c.FechaHora)
                                .AsQueryable();

            if (User.IsInRole("Guardia"))
            {
                // Solo los del guardia
                query = query.Where(c => c.UsuarioId == userId);
            }
            else if (User.IsInRole("Administrador") || User.IsInRole("Consultor"))
            {
                // Obtener CompanyId del usuario actual
                var user = await _userManager.GetUserAsync(User);
                var companyId = user?.CompanyId;

                // Solo los checklist de su empresa
                query = query.Where(c => c.Usuario.CompanyId == companyId);
            }
            else if (User.IsInRole("Superusuario"))
            {
                // El superusuario puede ver todo (sin filtros)
            }

            return View(await query.ToListAsync());
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Checklists
                .Include(c => c.Puntos)
                .Include(c => c.Fotos)
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (item == null) return NotFound();
            return View(item);
        }

        public IActionResult Create()
        {
            var vm = new Checklist
            {
                Puntos = Descripciones17
                    .Select(d => new PuntoChecklist { Descripcion = d })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Checklist checklist,
            List<IFormFile> photos,
            string? FotoDesdeCamara = null)
        {
            if (!ModelState.IsValid)
            {
                var completadosJson = Request.Form["CompletadosJson"];
                ViewBag.Completados = string.IsNullOrEmpty(completadosJson)
                    ? new bool[checklist.Puntos.Count]
                    : JsonConvert.DeserializeObject<bool[]>(completadosJson);

                return View(checklist);
            }




            checklist.FechaHora = DateTime.Now;
            checklist.UsuarioId = _userManager.GetUserId(User);

            // --- Procesar FotoDesdeCamara ---
            // --- Procesar múltiples rutas desde cámara ---
            if (checklist.IncluirFotoCamara && FotoDesdeCamara != null)
            {
                checklist.Fotos ??= new List<FotoChecklist>();

                // Admite múltiples imágenes separadas por |
                var rutas = FotoDesdeCamara.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var ruta in rutas)
                {
                    checklist.Fotos.Add(new FotoChecklist { Url = ruta });
                }
            }



            // --- Guardar fotos generales ---
            if (photos?.Any() == true)
            {
                var webRoot = _env.WebRootPath
                               ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var uploads = Path.Combine(webRoot, "uploads");
                Directory.CreateDirectory(uploads);

                checklist.Fotos ??= new List<FotoChecklist>();
                foreach (var file in photos)
                {
                    if (file.Length > 0)
                    {
                        var fn = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var path = Path.Combine(uploads, fn);
                        using var image = SixLabors.ImageSharp.Image.Load(file.OpenReadStream());
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(800, 800) // Máximo 800x800 px
                        }));
                        await image.SaveAsJpegAsync(path);

                        checklist.Fotos.Add(new FotoChecklist { Url = "/uploads/" + fn });
                    }
                }
            }

            // --- Guardar fotos de cada punto ---
            {
                var webRoot = _env.WebRootPath
                                ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var carpetaP = Path.Combine(webRoot, "uploads", "checklist");
                Directory.CreateDirectory(carpetaP);

                var puntosConFoto = new List<PuntoChecklist>();
                for (int i = 0; i < checklist.Puntos.Count; i++)
                {
                    var punto = checklist.Puntos[i];
                    var key = $"Puntos[{i}].Foto";
                    var file = Request.Form.Files[key];

                    if (file != null && file.Length > 0)
                    {
                        var fn = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var fp = Path.Combine(carpetaP, fn);
                        using var st = new FileStream(fp, FileMode.Create);
                        await file.CopyToAsync(st);
                        punto.FotoRuta = "/uploads/checklist/" + fn;
                    }

                    puntosConFoto.Add(punto);
                }

                checklist.Puntos = puntosConFoto;
            }

            // --- Persistir en BD ---
            try
            {
                _context.Add(checklist);
                await _context.SaveChangesAsync();
                TempData["Action"] = "success";
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                TempData["Action"] = "error";
                checklist.Puntos = Descripciones17
                    .Select(d => new PuntoChecklist { Descripcion = d })
                    .ToList();
                return View(checklist);
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CapturaCamara()
        {
            var companyId = User.FindFirst("CompanyId")?.Value;

            if (string.IsNullOrEmpty(companyId))
                return BadRequest("No se pudo determinar la empresa.");

            var camaras = await _context.Camaras
                .Where(c => c.EmpresaId == companyId)
                .ToListAsync();

            if (!camaras.Any())
                return BadRequest("No hay cámaras configuradas para esta empresa.");

            var resultados = new List<object>();
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", "camaras");
            Directory.CreateDirectory(folder);

            foreach (var (camara, index) in camaras.Select((c, i) => (c, i)))
            {
                int numero = index + 1;

                try
                {
                    var handler = new HttpClientHandler
                    {
                        Credentials = new NetworkCredential(camara.Usuario, camara.Contrasena)
                    };

                    using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                    var ruta = string.IsNullOrWhiteSpace(camara.RutaSnapshot)
                      ? "/ISAPI/Streaming/channels/1/picture"
                      : (camara.RutaSnapshot.StartsWith("/") ? camara.RutaSnapshot : "/" + camara.RutaSnapshot);

                    var url = $"http://{camara.IP}{ruta}";


                    var resp = await client.GetAsync(url);

                    if (!resp.IsSuccessStatusCode)
                        throw new HttpRequestException($"Código de estado: {resp.StatusCode}");

                    var data = await resp.Content.ReadAsByteArrayAsync();

                    var fileName = $"camara_{camara.Id}_{DateTime.Now.Ticks}.jpg";
                    var fullPath = Path.Combine(folder, fileName);
                    await System.IO.File.WriteAllBytesAsync(fullPath, data);

                    resultados.Add(new
                    {
                        Numero = numero,
                        Estado = "ok",
                        Url = "/uploads/camaras/" + fileName
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al capturar imagen de la cámara {numero} ({camara.IP}): {ex.Message}");
                    Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");

                    resultados.Add(new
                    {
                        Numero = numero,
                        Estado = "error",
                        Mensaje = $"Cámara {numero} sin conexión ({ex.GetType().Name}): {ex.Message}"
                    });
                }
            }

            return new JsonResult(resultados, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });
        }

    }
}