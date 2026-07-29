using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlanningTool.NavExtension.Services;

namespace PlanningTool.NavExtension.Pages
{
    public class AccessDeniedModel : PageModel
    {
        private readonly IUserPermissionService _permissionService;

        public string ReturnUrl { get; set; } = "/Index";

        public AccessDeniedModel(IUserPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task OnGetAsync()
        {
            var userId = User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userId))
                return;

            var permission =
                await _permissionService.GetUserPermissionAsync(
                    userId.ToUpperInvariant());

            if (permission?.Procurement == true)
                ReturnUrl = "/Procurement/Index";
            else if (permission?.ImPex == true)
                ReturnUrl = "/Impex/Index";
        }
    }
}
