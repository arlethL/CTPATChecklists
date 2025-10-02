using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CTPATChecklists.Models
{
    public class AdminCreateViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string CompanyId { get; set; }

        [Required]
        public string CompanyName { get; set; }

        public string CompanyAddress { get; set; }

        public string Phone { get; set; }

        [EmailAddress]
        public string ContactEmail { get; set; }
    }

}
