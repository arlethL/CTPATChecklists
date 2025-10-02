// Models/EditRolesViewModel.cs
using System.Collections.Generic;

namespace CTPATChecklists.Models
{
    public class EditRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<RoleCheckbox> Roles { get; set; }
    }

    public class RoleCheckbox
    {
        public string RoleName { get; set; }
        public bool Selected { get; set; }
    }
}