using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using PlanningTool.NavExtension.Services;
using System.Security;

namespace PlanningTool.NavExtension.Pages.Expiration
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ExpirationMonitoringModel> WMSList { get; set; } = new();
        public List<ExpirationMonitoringModel> QAList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            WMSList = await _context.ExpirationMonitoring
             .FromSqlRaw("EXEC _spExpirationMonitoringWMS")
             .ToListAsync();

            QAList = await _context.ExpirationMonitoring
             .FromSqlRaw("EXEC _spExpirationMonitoringQA")
             .ToListAsync();

            return Page();
        }
    }
}
