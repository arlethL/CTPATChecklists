// Models/Branding.cs
using System.ComponentModel.DataAnnotations;

public class Branding
{
    public int Id { get; set; }

    [Required]
    public string CompanyId { get; set; }

    [Display(Name = "Nombre de la empresa")]
    public string CompanyName { get; set; }

    [Display(Name = "Logo")]
    public string LogoPath { get; set; }

    [Display(Name = "Color primario")]
    public string PrimaryColor { get; set; }

    [Display(Name = "Color secundario")]
    public string SecondaryColor { get; set; }

    [Display(Name = "Color de texto")]
    public string FontColor { get; set; }

    [Display(Name = "Tipografía")]
    public string FontFamily { get; set; }
}
