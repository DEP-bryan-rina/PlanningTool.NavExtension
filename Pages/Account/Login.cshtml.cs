using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlanningTool.NavExtension.Services;

namespace PlanningTool.NavExtension.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IUserPermissionService _userService;

        public LoginModel(IUserPermissionService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string userId = (User.Identity?.Name ?? string.Empty).ToUpper();

                var permission = await _userService.GetUserPermissionAsync(userId);

                if (permission.ImPex)
                {
                    return RedirectToPage("/ImPex/Index");
                }

                if (permission.Procurement)
                {
                    return RedirectToPage("/Procurement/Index");
                }

                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}