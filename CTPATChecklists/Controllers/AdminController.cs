// Controllers/AdminController.cs
using System.Linq;
using System.Threading.Tasks;
using CTPATChecklists.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTPATChecklists.Controllers
{
    // Sólo superusuarios y administradores pueden gestionar usuarios
    [Authorize(Roles = "Superusuario,Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;

        public AdminController(
            UserManager<ApplicationUser> userMgr,
            RoleManager<IdentityRole> roleMgr)
        {
            _userMgr = userMgr;
            _roleMgr = roleMgr;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            // Listamos todos los usuarios
            var users = await _userMgr.Users.ToListAsync();
            return View(users);
        }

        // GET: /Admin/EditRoles/{id}
        public async Task<IActionResult> EditRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var user = await _userMgr.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Traemos primero TODOS los roles y los roles del usuario
            var allRoles = await _roleMgr.Roles.ToListAsync();
            var userRoles = await _userMgr.GetRolesAsync(user);

            var vm = new EditRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = allRoles.Select(r => new RoleCheckbox
                {
                    RoleName = r.Name,
                    Selected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Admin/EditRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(EditRolesViewModel vm)
        {
            var user = await _userMgr.FindByIdAsync(vm.UserId);
            if (user == null)
                return NotFound();

            var currentRoles = await _userMgr.GetRolesAsync(user);
            var selectedRoles = vm.Roles.Where(r => r.Selected).Select(r => r.RoleName);

            // Remover roles no deseados
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            if (rolesToRemove.Any())
                await _userMgr.RemoveFromRolesAsync(user, rolesToRemove);

            // Añadir nuevos roles seleccionados
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            if (rolesToAdd.Any())
                await _userMgr.AddToRolesAsync(user, rolesToAdd);

            return RedirectToAction(nameof(Index));
        }
    }
}