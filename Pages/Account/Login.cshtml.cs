using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlanningTool.NavExtension.Services;
using System.DirectoryServices.AccountManagement;

namespace PlanningTool.NavExtension.Pages.Account
{
    [Authorize]
    public class LoginModel : PageModel
    {
        private readonly IUserPermissionService _permissionService;

        public LoginModel(IUserPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        //public void OnGet()
        //{
        //    UserName = User.Identity?.Name;

        //    var samAccountName = UserName.Split('\\')[1];

        //    using var context = new PrincipalContext(ContextType.Domain);

        //    var user = UserPrincipal.FindByIdentity(
        //        context,
        //        samAccountName);

        //    if (user != null)
        //    {
        //        FullName = user.DisplayName;
        //        Email = user.EmailAddress;
        //    }
        //}
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

            return RedirectToPage("/Procurement/Tagging");
        }
    }
}