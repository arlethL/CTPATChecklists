using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El campo Email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El campo Contraseña es obligatorio")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }

        // Cambiado a nullable para que no se considere obligatorio
        public string? ReturnUrl { get; set; }
    }
}