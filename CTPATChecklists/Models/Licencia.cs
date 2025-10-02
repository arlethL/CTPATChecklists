using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTPATChecklists.Models
{
    public class Licencia
    {
        public int Id { get; set; }

        [Required]
        public string CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Empresa Empresa { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaExpiracion { get; set; }

        [Required]
        public string CodigoActivacion { get; set; }

        public bool Activa { get; set; }

        public bool Notificado { get; set; } = false;

        public int DiasDuracion { get; set; }
        public bool FueCanjeada { get; set; } = false;

    }
}
