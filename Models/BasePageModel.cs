using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlanningTool.NavExtension.Pages;

public abstract class BasePageModel : PageModel
{
    protected bool HasImPex =>
        bool.TryParse(HttpContext.Session.GetString("ImPex"), out var value)
        && value;

    protected bool HasProcurement =>
        bool.TryParse(HttpContext.Session.GetString("Procurement"), out var value)
        && value;

    protected string UserId =>
        HttpContext.Session.GetString("UserId") ?? string.Empty;
}