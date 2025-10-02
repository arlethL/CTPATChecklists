using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models.ViewModels
{
    public class EmpresaRegistroViewModel
    {
        // 👤 Datos del administrador
        [Required(ErrorMessage = "El correo del administrador es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
        public string AdminEmail { get; set; }

        [Required(ErrorMessage = "La contraseña del administrador es obligatoria")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string AdminPassword { get; set; }

        // 🏢 Datos de la empresa
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        public string CompanyAddress { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "El correo de contacto es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
        public string Email { get; set; }

        // 🎨 Personalización (branding)
        public IFormFile LogoUpload { get; set; }

        [Required(ErrorMessage = "Selecciona un color primario")]
        public string PrimaryColor { get; set; }

        [Required(ErrorMessage = "Selecciona un color secundario")]
        public string SecondaryColor { get; set; }

        [Required(ErrorMessage = "Selecciona un color de texto")]
        public string FontColor { get; set; }

        [Required(ErrorMessage = "Selecciona una tipografía")]
        public string FontFamily { get; set; }

        // 🚫 Campo interno para uso del controlador (no se llena desde la vista)
        public string CompanyId { get; set; }
    }
}
