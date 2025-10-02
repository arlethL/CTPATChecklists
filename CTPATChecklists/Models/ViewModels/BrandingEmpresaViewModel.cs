namespace CTPATChecklists.Models.ViewModels
{
    public class BrandingEmpresaViewModel
    {
        public Branding Branding { get; set; }

        // Para mostrar info de la empresa en Branding/Index
        public string CompanyAddress { get; set; }
        public string Phone { get; set; }
        public string ContactEmail { get; set; }
    }


}
