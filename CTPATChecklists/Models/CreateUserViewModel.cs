using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmación de la contraseña es obligatoria")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Selecciona un rol")]
        public string SelectedRole { get; set; }

        public string CompanyId { get; set; } // No requerido aquí, se maneja en el controlador
        public string CompanyName { get; set; } // También opcional, usado sólo si aplica

        public List<string> AvailableRoles { get; set; } = new List<string>();
    }
}
