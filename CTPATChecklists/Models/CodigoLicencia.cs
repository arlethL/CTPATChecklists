using System;

namespace CTPATChecklists.Models
{
    public class CodigoLicencia
    {
        public int Id { get; set; }
        public string Codigo { get; set; } // Código alfanumérico
        public int DuracionDias { get; set; } // Duración del código
        public bool Usado { get; set; } = false;
        public string UsadoPorEmpresaId { get; set; }
        public DateTime? FechaUso { get; set; }
    }
}
