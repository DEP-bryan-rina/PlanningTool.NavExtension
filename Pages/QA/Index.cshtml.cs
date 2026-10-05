using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Models;
using System.Data;

namespace PlanningTool.NavExtension.Pages.QA
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public IndexModel(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public List<QADashboardItem> Items { get; set; } = new();

        public decimal DefectRate { get; set; }
        public decimal Completion { get; set; }
        public decimal QualityRating { get; set; }
        public decimal LeadTime { get; set; }
        public int LeadTimeDays { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                Items = await _context
                    .Set<QADashboardItem>()
                    .FromSqlRaw("EXEC dbo._spGetQADashboard;")
                    .ToListAsync();

                // Totals
                var totalReceived = Items.Sum(x => x.QtyReceived);
                var totalGood = Items.Sum(x => x.QtyGood);
                var totalReject = Items.Sum(x => x.QtyReject);
                var totalRepair = Items.Sum(x => x.QtyRepair);

                var totalDefective = totalReject + totalRepair;
                var totalFinishedInspection = totalGood + totalReject + totalRepair;

                // KPI Calculations
                if (totalReceived > 0)
                {
                    DefectRate = decimal.Round(
                        (totalDefective / totalReceived) * 100,
                        1);

                    Completion = decimal.Round(
                        (totalFinishedInspection / totalReceived) * 100,
                        1);

                    QualityRating = decimal.Round(
                        100 - DefectRate,
                        1);
                }
                else
                {
                    DefectRate = 0m;
                    Completion = 0m;
                    QualityRating = 0m;
                }

                // Average QA Lead Time
                LeadTime = Items.Any()
                    ? decimal.Round(
                        Items.Average(x => (decimal)x.LeadTimeDays),
                        1)
                    : 0m;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
        }
    }
}
