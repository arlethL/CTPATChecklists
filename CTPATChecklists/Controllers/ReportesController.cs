// Controllers/ReportesController.cs
using CTPATChecklists.Data;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// EPPlus para Excel
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;

using OfficeOpenXml;
using OfficeOpenXml.Style;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing; // ← Importante para 'Image.FromStream'

// PdfSharpCore para PDF
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Color = System.Drawing.Color;


namespace CTPATChecklists.Controllers
{
    [Authorize(Roles = "Administrador,Superusuario,Guardia")]
    public class ReportesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ReportesController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Filtros = new
            {
                desde = (DateTime?)null,
                hasta = (DateTime?)null,
                placa = "",
                empresa = "",
                operador = ""
            };
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(
            DateTime? desde,
            DateTime? hasta,
            string placa,
            string empresa,
            string operador,
            int[] selectedIds,
            string action
)
        {
            // Obtener el CompanyId del usuario logueado
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            var companyId = user?.CompanyId;

            if (action == "buscar")
            {
                var resultados = await Filtrar(desde, hasta, placa, empresa, operador, companyId);
                ViewBag.Resultados = resultados;
                ViewBag.Filtros = new { desde, hasta, placa, empresa, operador };
                return View();
            }

            if ((action == "exportarExcel" || action == "exportarPdf")
                && (selectedIds == null || !selectedIds.Any()))
            {
                ModelState.AddModelError("", "Debes seleccionar al menos un checklist.");
                var resultados = await Filtrar(desde, hasta, placa, empresa, operador, companyId);
                ViewBag.Resultados = resultados;
                ViewBag.Filtros = new { desde, hasta, placa, empresa, operador };
                return View();
            }

            var datos = await _db.Checklists
                .Include(c => c.Usuario)
                .Include(c => c.Puntos)
                .Include(c => c.Fotos)
                .Where(c => selectedIds.Contains(c.Id) &&
                            (companyId == null || c.Usuario.CompanyId == companyId))
                .OrderByDescending(c => c.FechaHora)
                .ToListAsync();

            if (action == "exportarExcel")
                return ExportToExcel(datos);

            if (action == "exportarPdf")
            {
                if (datos.Count == 1)
                    return Json(new { success = true, message = "Usar modal" }); // opcional: puedes quitar este bloque
                return ExportPdfZip(datos);
            }


            return RedirectToAction(nameof(Index));
        }


        private IQueryable<Checklist> BaseQuery(
             DateTime? desde, DateTime? hasta,
             string placa, string empresa, string operador,
             string? companyId)
        {
            var q = _db.Checklists
                .Include(c => c.Usuario) // necesario para acceder a CompanyId
                .AsQueryable();

            // Aplicar filtro por empresa (CompanyId)
            if (!string.IsNullOrEmpty(companyId))
                q = q.Where(c => c.Usuario.CompanyId == companyId);

            if (desde.HasValue) q = q.Where(c => c.FechaHora >= desde.Value.Date);
            if (hasta.HasValue) q = q.Where(c => c.FechaHora <= hasta.Value.Date.AddDays(1).AddSeconds(-1));
            if (!string.IsNullOrEmpty(placa)) q = q.Where(c => c.Placa.Contains(placa));
            if (!string.IsNullOrEmpty(empresa)) q = q.Where(c => c.Empresa.Contains(empresa));
            if (!string.IsNullOrEmpty(operador)) q = q.Where(c => c.Operador.Contains(operador));

            return q.OrderByDescending(c => c.FechaHora);
        }


        private Task<Checklist[]> Filtrar(
             DateTime? desde, DateTime? hasta,
             string placa, string empresa, string operador,
             string? companyId) =>
             BaseQuery(desde, hasta, placa, empresa, operador, companyId)

                .Include(c => c.Puntos)
                .Include(c => c.Fotos)
                .ToArrayAsync();

        // === Exportar Excel ===

        private FileContentResult ExportToExcel(List<Checklist> datos)
        {
            // EPPlus 6.2.5 ya no necesita asignar LicenseContext si lo hiciste en Program.cs

            using var pkg = new ExcelPackage();
            foreach (var c in datos)
            {
                var sheet = pkg.Workbook.Worksheets.Add($"{c.Placa}_{c.FechaHora:yyyyMMddHHmm}");

                sheet.View.FreezePanes(9, 1);
                sheet.DefaultRowHeight = 18;
                sheet.Cells.Style.Font.Name = "Calibri";
                sheet.Cells.Style.Font.Size = 11;

                // Título
                sheet.Cells["A1:D1"].Merge = true;
                sheet.Cells[1, 1].Value = "CHECKLIST CTPAT";
                sheet.Cells[1, 1].Style.Font.Size = 16;
                sheet.Cells[1, 1].Style.Font.Bold = true;
                sheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Datos generales extendidos
                sheet.Cells[2, 1].Value = "Fecha:";
                sheet.Cells[2, 2].Value = c.FechaHora.ToString("yyyy-MM-dd HH:mm");

                sheet.Cells[3, 1].Value = "Placa:";
                sheet.Cells[3, 2].Value = c.Placa;

                sheet.Cells[4, 1].Value = "Operador:";
                sheet.Cells[4, 2].Value = c.Operador;

                sheet.Cells[5, 1].Value = "Empresa:";
                sheet.Cells[5, 2].Value = c.Empresa;

                sheet.Cells[6, 1].Value = "Movimiento:";
                sheet.Cells[6, 2].Value = c.EsEntrada == true ? "Entrada" : "Salida";

                sheet.Cells[7, 1].Value = "Folio Hoja Viajera:";
                sheet.Cells[7, 2].Value = c.FolioHojaViajera;

                sheet.Cells[8, 1].Value = "Folio Inspección Origen:";
                sheet.Cells[8, 2].Value = c.FolioInspeccionOrigen;

                sheet.Cells[9, 1].Value = "Folio Inspección:";
                sheet.Cells[9, 2].Value = c.FolioInspeccion;

                sheet.Cells[10, 1].Value = "Línea:";
                sheet.Cells[10, 2].Value = c.Linea;

                sheet.Cells[11, 1].Value = "Estado:";
                sheet.Cells[11, 2].Value = c.Estado;

                sheet.Cells[12, 1].Value = "Sucursal Origen:";
                sheet.Cells[12, 2].Value = c.SucursalOrigen;

                sheet.Cells[13, 1].Value = "Sucursal Destino:";
                sheet.Cells[13, 2].Value = c.SucursalDestino;

                sheet.Cells[14, 1].Value = "Fecha/Hora Salida:";
                sheet.Cells[14, 2].Value = c.FechaHoraSalida?.ToString("g");

                sheet.Cells[15, 1].Value = "Fecha/Hora Entrada:";
                sheet.Cells[15, 2].Value = c.FechaHoraEntrada?.ToString("g");

                sheet.Cells[16, 1].Value = "Hora Inicio Inspección:";
                sheet.Cells[16, 2].Value = c.HoraInicioInspeccion?.ToString("t");

                sheet.Cells[17, 1].Value = "Hora Final Inspección:";
                sheet.Cells[17, 2].Value = c.HoraFinalInspeccion?.ToString("t");

                sheet.Cells[18, 1].Value = "Fianza:";
                sheet.Cells[18, 2].Value = c.Fianza;

                sheet.Cells[19, 1].Value = "Marca del Tractor:";
                sheet.Cells[19, 2].Value = c.MarcaTractor;

                sheet.Cells[20, 1].Value = "Caja/Tráiler ID:";
                sheet.Cells[20, 2].Value = c.IdCajaTrailer;

                sheet.Cells[21, 1].Value = "Remolque Año:";
                sheet.Cells[21, 2].Value = c.RemolqueAnio;

                sheet.Cells[22, 1].Value = "Remolque VIN:";
                sheet.Cells[22, 2].Value = c.RemolqueVIN;

                sheet.Cells[23, 1].Value = "Remolque Marca:";
                sheet.Cells[23, 2].Value = c.RemolqueMarca;

                sheet.Cells[24, 1].Value = "Contenedor Año:";
                sheet.Cells[24, 2].Value = c.ContenedorAnio;

                sheet.Cells[25, 1].Value = "Contenedor VIN:";
                sheet.Cells[25, 2].Value = c.ContenedorVIN;

                sheet.Cells[26, 1].Value = "Contenedor Marca:";
                sheet.Cells[26, 2].Value = c.ContenedorMarca;

                sheet.Cells[27, 1].Value = "Sello Carga/Vacía:";
                sheet.Cells[27, 2].Value = c.SelloCarga;

                sheet.Cells[28, 1].Value = "Sello Adicional:";
                sheet.Cells[28, 2].Value = c.SelloAdicional;

                sheet.Cells[29, 1].Value = "Hora/Lugar/Motivo Retiro Sello:";
                sheet.Cells[29, 2].Value = c.HoraLugarMotivoRetiroSello;


                // Encabezado tabla
                int start = 31;
                var hdr = sheet.Cells[start, 1, start, 4];
                hdr.LoadFromArrays(new[] { new[] { "Descripción", "Cumple", "Observaciones", "Foto" } });
                hdr.Style.Font.Bold = true;
                hdr.Style.Fill.PatternType = ExcelFillStyle.Solid;
                hdr.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                hdr.Style.Font.Color.SetColor(Color.White);
                hdr.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                hdr.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                // Contenido por fila
                int row = start + 1;
                foreach (var p in c.Puntos)
                {
                    sheet.Cells[row, 1].Value = p.Descripcion;
                    sheet.Cells[row, 2].Value = p.Cumple == true ? "✔ Sí" : "✖ No";
                    sheet.Cells[row, 3].Value = p.Observaciones ?? "-";

                    var rng = sheet.Cells[row, 1, row, 4];
                    rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    if (!string.IsNullOrEmpty(p.FotoRuta))
                    {
                        var rel = p.FotoRuta.TrimStart('/');
                        var path = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(path))
                        {
                            var pic = sheet.Drawings.AddPicture($"punto_{row}", new FileInfo(path));

                            // Fila - 1 porque EPPlus usa índice base 0 para imágenes
                            // Columna 3 (índice base 0) = columna 4 (Foto)
                            pic.SetPosition(row - 1, 5, 3, 5); // fila, offsetY, columna, offsetX
                            pic.SetSize(80, 80);

                            sheet.Row(row).Height = 65;
                        }
                    }

                    row++;
                }

                // Galería de imágenes (Fotos generales del checklist)
                if (c.Fotos.Any())
                {
                    row += 2;
                    sheet.Cells[row, 1].Value = "Fotos generales del checklist:";
                    sheet.Cells[row, 1].Style.Font.Bold = true;
                    row++;

                    int colImg = 1;
                    foreach (var f in c.Fotos)
                    {
                        var rel = f.Url.TrimStart('/');
                        var path = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(path))
                        {
                            var pic = sheet.Drawings.AddPicture($"img_{row}_{colImg}", new FileInfo(path));
                            pic.SetPosition(row - 1, 0, colImg - 1, 0);
                            pic.SetSize(100, 100);
                            colImg++;

                            if (colImg > 3)
                            {
                                colImg = 1;
                                row += 6;
                            }
                        }
                    }
                }

                // Ajuste de anchos de columnas
                sheet.Column(1).Width = 50; // Descripción
                sheet.Column(2).Width = 12; // Cumple
                sheet.Column(3).Width = 40; // Observaciones
                sheet.Column(4).Width = 25; // Foto

                // FIRMAS (base64) al final
                int firmaRowStart = sheet.Dimension.End.Row + 3;
                sheet.Cells[firmaRowStart, 1].Value = "Firmas:";
                sheet.Cells[firmaRowStart, 1].Style.Font.Bold = true;

                // Títulos
                var firmas = new[]
                {
                new { Titulo = "Operador", Base64 = c.FirmaOperadorOrigen },
                new { Titulo = "Oficial", Base64 = c.FirmaOficial },
                new { Titulo = "Supervisor", Base64 = c.FirmaSupervisor }
            };

                int colFirma = 1;
                foreach (var firma in firmas)
                {
                    int filaTitulo = firmaRowStart + 1;
                    int filaImagen = firmaRowStart + 2;

                    // Título de cada firma
                    sheet.Cells[filaTitulo, colFirma].Value = firma.Titulo;
                    sheet.Cells[filaTitulo, colFirma].Style.Font.Bold = true;

                    // Imagen de firma (base64)
                    if (!string.IsNullOrWhiteSpace(firma.Base64))
                    {
                        try
                        {
                            byte[] imageBytes = Convert.FromBase64String(firma.Base64.Split(',')[1]);
                            var ms = new MemoryStream(imageBytes);
                            ms.Position = 0; // Asegurar que el stream está al inicio

                            var picture = sheet.Drawings.AddPicture($"firma_{colFirma}_{c.Placa}", ms);

                            // Posición de la imagen
                            picture.SetPosition(filaImagen - 1, 0, colFirma - 1, 0); // fila, offsetY, col, offsetX
                            picture.SetSize(120, 60);
                        }
                        catch
                        {
                            sheet.Cells[filaImagen, colFirma].Value = "Firma inválida";
                            sheet.Cells[filaImagen, colFirma].Style.Font.Color.SetColor(Color.Red);
                        }
                    }

                    else
                    {
                        sheet.Cells[filaImagen, colFirma].Value = "Sin firma";
                        sheet.Cells[filaImagen, colFirma].Style.Font.Italic = true;
                    }

                    colFirma++;
                }

            }



            var bytes = pkg.GetAsByteArray();
            var fileName = $"reportes_{DateTime.Now:yyyyMMddHHmm}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // === Exportar un solo PDF ===
        [HttpPost]
        public IActionResult ExportSinglePdfModal(int id)
        {
            var checklist = _db.Checklists
                .Include(c => c.Puntos)
                .Include(c => c.Fotos)
                .FirstOrDefault(c => c.Id == id);

            if (checklist == null)
                return NotFound();

            var tempFolder = Path.Combine(_env.WebRootPath, "pdf_preview");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            var fileName = $"preview_{checklist.Id}_{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(tempFolder, fileName);

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                BuildPdf(checklist, fs);
            }

            var fileUrl = $"/pdf_preview/{fileName}";
            return Json(new { success = true, url = fileUrl });
        }


        public IActionResult VistaPreviaPdf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            ViewBag.FilePath = $"/pdf_preview/{fileName}";
            return View();
        }


        // === Exportar múltiples como ZIP de PDFs ===
        private FileContentResult ExportPdfZip(List<Checklist> datos)
        {
            using var zipMs = new MemoryStream();
            using var zip = new ZipArchive(zipMs, ZipArchiveMode.Create, true);
            foreach (var c in datos)
            {
                using var pdfMs = new MemoryStream();
                BuildPdf(c, pdfMs);
                var entryName = $"report_{c.FechaHora:yyyyMMddHHmm}_{c.Placa}.pdf";
                var entry = zip.CreateEntry(entryName);
                using var es = entry.Open();
                pdfMs.Seek(0, SeekOrigin.Begin);
                pdfMs.CopyTo(es);
            }
            zipMs.Seek(0, SeekOrigin.Begin);
            return File(zipMs.ToArray(), "application/zip",
                $"reportes_{DateTime.Now:yyyyMMddHHmm}.zip");
        }

        // === Construye el PDF con formato profesional y armonizado ===
        private void BuildPdf(Checklist checklist, Stream stream)
        {
            var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var tf = new XTextFormatter(gfx);

            // Fuentes y medidas
            var fontRegular = new XFont("Arial", 10, XFontStyle.Regular);
            var fontBold = new XFont("Arial", 10, XFontStyle.Bold);
            var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);
            var fontSection = new XFont("Arial", 12, XFontStyle.Bold);

            double margin = 40;
            double y = 40;
            double lineHeight = 18;

            // Colores
            XBrush headerBarBrush = new XSolidBrush(XColor.FromArgb(0, 51, 102));     // Azul oscuro
            XBrush sectionHeaderBrush = new XSolidBrush(XColor.FromArgb(225, 240, 255));  // Azul muy suave
            XBrush labelBrush = new XSolidBrush(XColor.FromArgb(240, 240, 240));  // Gris claro
            XBrush valueBrush = XBrushes.White;
            XPen dividerPen = new XPen(XColors.LightGray, 0.5);

            // Helpers
            void NuevaPagina()
            {
                page = doc.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                tf = new XTextFormatter(gfx);
                y = margin;
            }

            void AsegurarEspacio(double altoNecesario)
            {
                if (y + altoNecesario > page.Height - margin)
                    NuevaPagina();
            }

            void DrawSectionHeader(string title)
            {
                AsegurarEspacio(lineHeight + 12);
                gfx.DrawRectangle(sectionHeaderBrush, new XRect(margin, y, page.Width - 2 * margin, lineHeight + 6));
                gfx.DrawString(title, fontSection, XBrushes.Black,
                    new XRect(margin + 10, y + 4, page.Width - 2 * margin, lineHeight), XStringFormats.TopLeft);
                y += lineHeight + 12;
            }

            // Método para campos alineados (etiqueta + valor)
            // === ENCABEZADO SUPERIOR ===
            gfx.DrawRectangle(headerBarBrush, new XRect(0, y, page.Width, 50));
            gfx.DrawString("REPORTE DE INSPECCIÓN C-TPAT", fontTitle, XBrushes.White,
            new XRect(0, y + 15, page.Width, 30), XStringFormats.Center);
            y += 70;

            // === TODOS LOS DATOS COMO TABLA CONTINUA ===
            var campos = new List<(string Etiqueta, string Valor)>
            {
                // DATOS GENERALES
                ("Placa", checklist.Placa),
                ("Operador", checklist.Operador),
                ("Empresa", checklist.Empresa),
                ("Movimiento", checklist.EsEntrada == true ? "Entrada" : "Salida"),

                // DATOS DEL VIAJE
                ("Folio Hoja Viajera", checklist.FolioHojaViajera),
                ("Folio Inspección Origen", checklist.FolioInspeccionOrigen),
                ("Folio Inspección", checklist.FolioInspeccion),
                ("Línea", checklist.Linea),
                ("Estado", checklist.Estado),
                ("Sucursal Origen", checklist.SucursalOrigen),
                ("Sucursal Destino", checklist.SucursalDestino),

                // FECHAS Y HORARIOS
                ("Fecha/Hora Salida", checklist.FechaHoraSalida?.ToString("g") ?? "-"),
                ("Fecha/Hora Entrada", checklist.FechaHoraEntrada?.ToString("g") ?? "-"),
                ("Hora Inicio Inspección", checklist.HoraInicioInspeccion?.ToString("t") ?? "-"),
                ("Hora Final Inspección", checklist.HoraFinalInspeccion?.ToString("t") ?? "-"),

                // DATOS DEL VEHÍCULO
                ("Fianza", checklist.Fianza),
                ("Marca del Tractor", checklist.MarcaTractor),
                ("Caja/Tráiler ID", checklist.IdCajaTrailer),

                // DATOS DEL REMOLQUE
                ("Año Remolque", checklist.RemolqueAnio?.ToString() ?? "-"),
                ("VIN Remolque", checklist.RemolqueVIN),
                ("Marca Remolque", checklist.RemolqueMarca),

                // DATOS DEL CONTENEDOR
                ("Año Contenedor", checklist.ContenedorAnio?.ToString() ?? "-"),
                ("VIN Contenedor", checklist.ContenedorVIN),
                ("Marca Contenedor", checklist.ContenedorMarca),
                ("Sello Carga/Vacía", checklist.SelloCarga),
                ("Sello Adicional", checklist.SelloAdicional),
                ("Hora/Lugar/Motivo Retiro", checklist.HoraLugarMotivoRetiroSello)
            };

                // === DIBUJAR EN FORMA DE TABLA 2x2 ===
                double tableLabelWidth = 130;
                double tableValueWidth = 160;
                double rowSpacing = 10;
                double xLeft = margin;
                double xRight = margin + tableLabelWidth + tableValueWidth + 50;

        for (int i = 0; i < campos.Count; i += 2)
        {
            AsegurarEspacio(lineHeight + rowSpacing);

            // Campo izquierdo
            var left = campos[i];
            gfx.DrawRectangle(labelBrush, new XRect(xLeft, y, tableLabelWidth, lineHeight));
            gfx.DrawString(left.Etiqueta, fontBold, XBrushes.Black, new XRect(xLeft + 5, y + 3, tableLabelWidth, lineHeight), XStringFormats.TopLeft);

            gfx.DrawRectangle(valueBrush, new XRect(xLeft + tableLabelWidth, y, tableValueWidth, lineHeight));
            gfx.DrawString(left.Valor ?? "-", fontRegular, XBrushes.Black, new XRect(xLeft + tableLabelWidth + 5, y + 3, tableValueWidth, lineHeight), XStringFormats.TopLeft);

            // Campo derecho (si existe)
            if (i + 1 < campos.Count)
            {
                var right = campos[i + 1];
                gfx.DrawRectangle(labelBrush, new XRect(xRight, y, tableLabelWidth, lineHeight));
                gfx.DrawString(right.Etiqueta, fontBold, XBrushes.Black, new XRect(xRight + 5, y + 3, tableLabelWidth, lineHeight), XStringFormats.TopLeft);

                gfx.DrawRectangle(valueBrush, new XRect(xRight + tableLabelWidth, y, tableValueWidth, lineHeight));
                gfx.DrawString(right.Valor ?? "-", fontRegular, XBrushes.Black, new XRect(xRight + tableLabelWidth + 5, y + 3, tableValueWidth, lineHeight), XStringFormats.TopLeft);
            }

            y += lineHeight + rowSpacing;
        }


            // ====== PUNTOS REVISADOS (NUEVA PÁGINA) ======
            NuevaPagina(); // forzar siempre a página nueva

            // Encabezado de sección
            DrawSectionHeader("PUNTOS REVISADOS");

            // Encabezados de tabla
            double tablaAncho = page.Width - 2 * margin;
            double wNum = 28;
            double wDesc = tablaAncho * 0.50;
            double wEstado = tablaAncho * 0.14;
            double wObs = tablaAncho - (wNum + wDesc + wEstado);
            double hRow = 20;

            void DrawPointsTableHeader()
            {
                AsegurarEspacio(hRow + 2);
                double x = margin;

                gfx.DrawRectangle(sectionHeaderBrush, new XRect(x, y, wNum, hRow));
                gfx.DrawString("#", fontBold, XBrushes.Black, new XRect(x + 3, y + 3, wNum - 6, hRow), XStringFormats.TopLeft);
                x += wNum;

                gfx.DrawRectangle(sectionHeaderBrush, new XRect(x, y, wDesc, hRow));
                gfx.DrawString("Descripción", fontBold, XBrushes.Black, new XRect(x + 6, y + 3, wDesc - 10, hRow), XStringFormats.TopLeft);
                x += wDesc;

                gfx.DrawRectangle(sectionHeaderBrush, new XRect(x, y, wEstado, hRow));
                gfx.DrawString("Estado", fontBold, XBrushes.Black, new XRect(x + 6, y + 3, wEstado - 10, hRow), XStringFormats.TopLeft);
                x += wEstado;

                gfx.DrawRectangle(sectionHeaderBrush, new XRect(x, y, wObs, hRow));
                gfx.DrawString("Observaciones", fontBold, XBrushes.Black, new XRect(x + 6, y + 3, wObs - 10, hRow), XStringFormats.TopLeft);

                y += hRow;
            }

            DrawPointsTableHeader();

            int index = 1;
            foreach (var punto in checklist.Puntos)
            {
                // Salto si no cabe la fila + posible imagen
                AsegurarEspacio(hRow + 140);
                // Redibujar encabezado si inició nueva página
                if (y == margin) { DrawSectionHeader("PUNTOS REVISADOS"); DrawPointsTableHeader(); }

                double x = margin;
                string estadoTexto = punto.Cumple == true ? "Cumple" : "No cumple";

                // Zebra light
                if (index % 2 == 0)
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(250, 250, 250)), new XRect(x, y, tablaAncho, hRow));

                // # 
                gfx.DrawString(index.ToString(), fontRegular, XBrushes.Black, new XRect(x + 3, y + 3, wNum - 6, hRow), XStringFormats.TopLeft);
                x += wNum;

                // Descripción
                gfx.DrawString((punto.Descripcion ?? "").Replace("  ", " "), fontRegular, XBrushes.Black,
                    new XRect(x + 6, y + 3, wDesc - 10, hRow), XStringFormats.TopLeft);
                x += wDesc;

                // Estado
                gfx.DrawString(estadoTexto, fontRegular, XBrushes.Black, new XRect(x + 6, y + 3, wEstado - 10, hRow), XStringFormats.TopLeft);
                x += wEstado;

                // Observaciones
                gfx.DrawString(punto.Observaciones ?? "", fontRegular, XBrushes.Black, new XRect(x + 6, y + 3, wObs - 10, hRow), XStringFormats.TopLeft);

                y += hRow;

                // Imagen por punto (si existe)
                if (!string.IsNullOrWhiteSpace(punto.FotoRuta))
                {
                    var ruta = Path.Combine(_env.WebRootPath, punto.FotoRuta.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(ruta))
                    {
                        using var imgStream = System.IO.File.OpenRead(ruta);
                        var img = XImage.FromStream(() => imgStream);

                        double imgW = 220;
                        double imgH = 140;

                        AsegurarEspacio(imgH + 10);
                        if (y == margin) { DrawSectionHeader("PUNTOS REVISADOS"); DrawPointsTableHeader(); }

                        gfx.DrawImage(img, margin + 10, y, imgW, imgH);
                        y += imgH + 8;
                    }
                }

                index++;
            }

            // ====== FIRMAS ======
            AsegurarEspacio(lineHeight + 100);
            if (y == margin) { /* ya limpio si saltó página */ }
            DrawSectionHeader("FIRMAS");

            // Dimensiones
            int firmaWidth = 130;
            int firmaHeight = 70;
            int espacio = 30;

            // Posiciones horizontales
            double startX = margin;
            double spacingX = firmaWidth + espacio;

            // Lista de firmas
            var firmas = new[]
            {
                 new { Titulo = "Operador", Base64 = checklist.FirmaOperadorOrigen },
                 new { Titulo = "Oficial", Base64 = checklist.FirmaOficial },
                 new { Titulo = "Supervisor", Base64 = checklist.FirmaSupervisor }
            };

            // Dibujar las firmas
            for (int i = 0; i < firmas.Length; i++)
            {
                double posX = startX + (i * spacingX);

                // Título
                gfx.DrawString(firmas[i].Titulo, fontRegular, XBrushes.Black, new XRect(posX, y, firmaWidth, 20), XStringFormats.TopLeft);

                // Imagen (si existe)
                if (!string.IsNullOrWhiteSpace(firmas[i].Base64))
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(firmas[i].Base64.Split(',')[1]);
                        using var ms = new MemoryStream(imageBytes);
                        var image = XImage.FromStream(() => ms);

                        gfx.DrawImage(image, posX, y + 18, firmaWidth, firmaHeight);
                    }
                    catch
                    {
                        gfx.DrawString("Firma inválida", fontRegular, XBrushes.Red, new XRect(posX, y + 25, firmaWidth, 20), XStringFormats.TopLeft);
                    }
                }
                else
                {
                    gfx.DrawString("Sin firma", fontRegular, XBrushes.Gray, new XRect(posX, y + 25, firmaWidth, 20), XStringFormats.TopLeft);
                }
            }
            NuevaPagina(); // forzar siempre a página nueva

            // ====== FOTOS DESDE CÁMARAS IP ======
            if (checklist.IncluirFotoCamara && checklist.Fotos != null)
            {
                var fotosCamara = checklist.Fotos.Where(f => f.Url != null && f.Url.Contains("/uploads/camaras/")).ToList();
                if (fotosCamara.Any())
                {
                    AsegurarEspacio(lineHeight + 12);
                    if (y == margin) { /* no-op */ }  // ya viene limpio si saltó página
                    DrawSectionHeader("FOTOS DESDE CÁMARAS IP");

                    // Mostrar en 2 columnas (grid simple)
                    double imgW = (page.Width - 2 * margin - 20) / 2; // dos por fila
                    double imgH = imgW * 0.65;
                    double x1 = margin;
                    double x2 = margin + imgW + 20;
                    int col = 0;

                    foreach (var foto in fotosCamara)
                    {
                        var ruta = Path.Combine(_env.WebRootPath, foto.Url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (!System.IO.File.Exists(ruta)) continue;

                        using var imgStream = System.IO.File.OpenRead(ruta);
                        var img = XImage.FromStream(() => imgStream);

                        AsegurarEspacio(imgH + 10);
                        if (y == margin) DrawSectionHeader("FOTOS DESDE CÁMARAS IP");

                        double x = (col % 2 == 0) ? x1 : x2;
                        gfx.DrawImage(img, x, y, imgW, imgH);
                        col++;

                        if (col % 2 == 0) y += imgH + 12; // siguiente fila
                    }

                    // Si quedó una sola en la última fila, agregar un pequeño margen
                    if (col % 2 == 1) y += imgH + 12;
                }
            }


            // Agregar espacio debajo
            y += firmaHeight + 40;





            doc.Save(stream);
        }






    }
}
