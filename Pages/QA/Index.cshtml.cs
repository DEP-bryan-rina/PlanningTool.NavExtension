using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Models;
using PlanningTool.NavExtension.Data;

namespace PlanningTool.NavExtension.Pages.QA
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<QADashboardItem> Items { get; set; } = new();

        public decimal DefectRate { get; set; }
        public decimal Completion { get; set; }
        public decimal QualityRating { get; set; }
        public decimal LeadTime { get; set; }

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

                var totalFinishedInspection =
                    totalGood +
                    totalReject +
                    totalRepair;

                // KPI Calculations
                if (totalReceived > 0)
                {
                    DefectRate = decimal.Round(
                        ((totalRepair + totalReject) / totalReceived) * 100,
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

                // Lead Time (Days)
                // Average days between Material Received and Inspection Completion
                var completedItems = Items
                    .Where(x =>
                        x.DateReceived.HasValue &&
                        x.InspectionCompletionDate.HasValue)
                    .ToList();

                LeadTime = completedItems.Any()
                    ? decimal.Round(
                        (decimal)completedItems.Average(x =>
                            (x.InspectionCompletionDate!.Value -
                             x.DateReceived!.Value).TotalDays),
                        1)
                    : 0m;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;                
            }
        }
    }
}