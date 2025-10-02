using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTPATChecklists.Models
{
    public class FotoChecklist
    {
        public int Id { get; set; }

        [Required]
        public int ChecklistId { get; set; }

        [ForeignKey(nameof(ChecklistId))]
        public Checklist Checklist { get; set; }

        [Required]
        public string Url { get; set; }    // Si usas cámara IP, podrías guardar la URL o ruta
    }
}