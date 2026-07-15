using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlanningTool.NavExtension.Services;

namespace PlanningTool.NavExtension.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IUserPermissionService _permissionService;

        public LoginModel(IUserPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Challenge();
            }

            var userId = User.Identity!.Name!.ToUpperInvariant();

            var permission =
                await _permissionService.GetUserPermissionAsync(userId);

            HttpContext.Session.SetString(
                "ImPex",
                permission.ImPex.ToString());

            HttpContext.Session.SetString(
                "Procurement",
                permission.Procurement.ToString());

            HttpContext.Session.SetString(
                "UserId",
                userId);

            if (!permission.ImPex && !permission.Procurement)
            {
                return RedirectToPage("/AccessDenied");
            }

            if (permission.ImPex)
            {
                return RedirectToPage("/ImPex/Index");
            }

            return RedirectToPage("/Procurement/Index");
        }
    }
}