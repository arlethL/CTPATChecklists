using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class Camara
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La IP es obligatoria")]
        public string IP { get; set; }

        [Required(ErrorMessage = "El Usuario es obligatorio")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "La Contraseña es obligatoria")]
        public string Contrasena { get; set; }

        public string EmpresaId { get; set; }

        [Required(ErrorMessage = "La ruta snapshot es obligatoria")]
        public string RutaSnapshot { get; set; }

    }
}
