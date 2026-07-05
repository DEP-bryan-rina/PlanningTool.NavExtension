using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlanningTool.NavExtension.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public void OnGet() => Unauthorized();
    }
}
