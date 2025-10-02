using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class Operador
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Nombre { get; set; }
    }
}
