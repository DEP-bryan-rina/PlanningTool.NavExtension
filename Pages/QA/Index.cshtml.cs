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
                    .FromSqlRaw("EXEC dbo._spGetQADashboard_ForDev;")
                    .ToListAsync();

                // ======================================================
                // KPI CALCULATIONS
                // ======================================================

                // Totals
                var totalReceived = Items.Sum(x => x.QtyReceived);
                var totalGood = Items.Sum(x => x.QtyGood);
                var totalReject = Items.Sum(x => x.QtyReject);
                var totalRepair = Items.Sum(x => x.QtyRepair);

                var totalDefective = totalReject + totalRepair;

                var totalInspected =
                    totalGood +
                    totalReject +
                    totalRepair;

                if (totalReceived > 0)
                {
                    // Defect Rate (%)
                    // How many received items have quality issues
                    DefectRate = decimal.Round(
                        (totalDefective / totalReceived) * 100,
                        1);

                    // Inspection Completion (%)
                    // How much of the received quantity has been inspected
                    Completion = decimal.Round(
                        (totalInspected / totalReceived) * 100,
                        1);

                    // Quality Yield (%)
                    // Pass rate of inspected items only
                    QualityRating = totalInspected > 0
                        ? decimal.Round(
                            (totalGood / totalInspected) * 100,
                            1)
                        : 0m;
                }
                else
                {
                    DefectRate = 0m;
                    Completion = 0m;
                    QualityRating = 0m;
                }

                // Average QA Lead Time (Days)
                LeadTime = Items
                    .Where(x => x.LeadTimeDays.HasValue)
                    .Any()
                        ? decimal.Round(
                            Items
                                .Where(x => x.LeadTimeDays.HasValue)
                                .Average(x => (decimal)x.LeadTimeDays.Value),
                            1)
                        : 0m;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetRepairMRPAsync(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Item Code is required."
                });
            }

            try
            {
                var itemCodeParam = new SqlParameter(
                    "@ItemCode",
                    SqlDbType.NVarChar,
                    20)
                {
                    Value = itemCode
                };

                var details = await _context
                    .Set<QARepairMRPDetail>()
                    .FromSqlRaw(
                        "EXEC [PTNAVEX].[dbo].[_spGetQARepairMRPDetails_ForDev] @ItemCode",
                        itemCodeParam)
                    .AsNoTracking()
                    .ToListAsync();

                return new JsonResult(new
                {
                    success = true,
                    itemCode,
                    data = details
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
