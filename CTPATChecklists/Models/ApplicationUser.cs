using Microsoft.AspNetCore.Identity;

namespace CTPATChecklists.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? CompanyId { get; set; }

        // Puedes tener también
        public string? DisplayName { get; set; }
    }
}