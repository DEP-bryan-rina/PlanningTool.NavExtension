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
        private readonly AppDbContext   _context;
        private readonly IConfiguration _config;

        public IndexModel(AppDbContext context, IConfiguration config)
        {
            _context    = context;
            _config     = config;
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
                var totalReceived   = Items.Sum(x => x.QtyReceived);
                var totalGood       = Items.Sum(x => x.QtyGood);
                var totalReject     = Items.Sum(x => x.QtyReject);
                var totalRepair     = Items.Sum(x => x.QtyRepair);

                var totalFinishedInspection = totalGood + totalReject + totalRepair;

                // KPI Calculations
                if (totalReceived > 0)
                {
                    DefectRate      = decimal.Round(((totalRepair + totalReject) / totalReceived) * 100,1);
                    Completion      = decimal.Round((totalFinishedInspection / totalReceived) * 100,1);
                    QualityRating   = decimal.Round(100 - DefectRate,1);
                }
                else
                {
                    DefectRate      = 0m;
                    Completion      = 0m;
                    QualityRating   = 0m;
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
        public IActionResult OnGetVendorQA()
        {
            List<VendorQA> result = new();

            using (SqlConnection con =
            new SqlConnection(
            _config.GetConnectionString("DevConnection")))
            {

                SqlCommand cmd  = new SqlCommand("_spGetVendorQADashboard",con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    result.Add(new VendorQA
                    {
                        VendorName  = dr["VendorName"].ToString(),
                        QtyReceived = dr["QtyReceived"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyReceived"]),
                        QtyRepair   = dr["QtyRepair"]   == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyRepair"]),
                        QtyReject   = dr["QtyReject"]   == DBNull.Value ? 0 : Convert.ToDecimal(dr["QtyReject"]),
                        DefectRate  = dr["DefectRate"]  == DBNull.Value ? 0 : Convert.ToDecimal(dr["DefectRate"])
                    });
                }
            }
            return new JsonResult(result);
        }
    }


    public class VendorQA
    {
        public string VendorName { get; set; }
        public decimal QtyReceived { get; set; }
        public decimal QtyRepair { get; set; }
        public decimal QtyReject { get; set; }
        public decimal DefectRate { get; set; }
    }
}
