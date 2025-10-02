using System;

namespace CTPATChecklists.Models
{
    public class CodigoLicencia
    {
        public int Id { get; set; }
        public string Codigo { get; set; } // C�digo alfanum�rico
        public int DuracionDias { get; set; } // Duraci�n del c�digo
        public bool Usado { get; set; } = false;
        public string UsadoPorEmpresaId { get; set; }
        public DateTime? FechaUso { get; set; }
    }
}
