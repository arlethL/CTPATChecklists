using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class Empresa
    {
        [Key]
        public string CompanyId { get; set; }

        public string Nombre { get; set; }          // (para mostrar nombre)
        public string Direccion { get; set; }       // (CompanyAddress)
        public string Telefono { get; set; }        // (Phone)
        public string Email { get; set; }           // (ContactEmail)

        public ICollection<Licencia> Licencias { get; set; }

    }
}