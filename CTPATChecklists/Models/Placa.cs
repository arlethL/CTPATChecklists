using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class Placa
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Valor { get; set; }
    }
}
