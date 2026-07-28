using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Pages.WMS
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }


        public void OnGet()
        {
            // Load Tagged POs
            ProcurementTagged = _context.ProcurementCenterBalanceView
            .Where(x => x._ptStatus == 1)
            .ToList();
        }

        public List<ProcurementCenterBalance> ProcurementTagged { get; set; } = new();
    }
}
