using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using PlanningTool.NavExtension.Services;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IUserPermissionService _permissionService;

    public IndexModel(AppDbContext context, IUserPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }
    public int TotalProcurements { get; set; }
    public int OpenProcurements { get; set; }
    public int CompletedProcurements { get; set; }

    public decimal TotalOutstandingQty { get; set; }

    public int TotalContainers { get; set; }

    public List<ProcurementCenterModel> RecentProcurements { get; set; } = new();

    public List<ImPexCenterModel> UpcomingContainers { get; set; } = new();

    public List<ImPexCenterModel> LatestShipments { get; set; } = new();
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
    //public async Task OnGetAsync()
    //{

    //    TotalProcurements = await _context.tblProcurementCenter.CountAsync();

    //    OpenProcurements = await _context.tblProcurementCenter
    //        .CountAsync(x => (x._ptBalanceQty ?? 0) > 0);

    //    CompletedProcurements = await _context.tblProcurementCenter
    //        .CountAsync(x => (x._ptBalanceQty ?? 0) <= 0);

    //    TotalOutstandingQty = await _context.tblProcurementCenter
    //        .SumAsync(x => x._ptOutstandingQty ?? 0);

    //    TotalContainers = await _context.tblImPexCenter
    //        .CountAsync();

    //    RecentProcurements = await _context.tblProcurementCenter
    //        .OrderByDescending(x => x._ptModifiedDate)
    //        .Take(5)
    //        .ToListAsync();

    //    UpcomingContainers = await _context.tblImPexCenter
    //        .Where(x => x._ptContainerETA != null)
    //        .OrderBy(x => x._ptContainerETA)
    //        .Take(5)
    //        .ToListAsync();

    //    LatestShipments = await _context.tblImPexCenter
    //        .OrderByDescending(x => x._ptModifiedDate)
    //        .Take(5)
    //        .ToListAsync();
    //}

}