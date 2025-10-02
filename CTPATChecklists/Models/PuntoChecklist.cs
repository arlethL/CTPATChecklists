#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTPATChecklists.Models
{
    public class PuntoChecklist
    {
        public int Id { get; set; }

        [Required]
        public int ChecklistId { get; set; }

        [ForeignKey(nameof(ChecklistId))]
        public Checklist? Checklist { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Debes indicar si cumple o no cumple")]
        public bool? Cumple { get; set; }

        // Ahora opcional: tipo anulable y sin [Required]
        [StringLength(250)]
        public string? Observaciones { get; set; }


        // Nueva propiedad para guardar la ruta del archivo de imagen
        [StringLength(300)]
        public string? FotoRuta { get; set; }

    }
}