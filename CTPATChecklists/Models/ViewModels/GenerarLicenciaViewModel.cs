using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models.ViewModels
{
    public class GenerarLicenciaViewModel
    {
        [Required]
        public string CompanyId { get; set; }

        [Required]
        public string Unidad { get; set; }  // "Dias", "Semanas", "Meses", "Anios"

        [Range(1, 12)]
        public int Cantidad { get; set; }

        public List<SelectListItem> Empresas { get; set; } = new();
    }
}
