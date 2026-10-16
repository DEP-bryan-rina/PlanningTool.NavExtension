using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Models
{
    [Keyless]
    public class QADashboardItem
    {        
        public string RRNo { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public DateTime? DateReceived { get; set; }

        public DateTime? DateQAStart { get; set; }

        public string UOM { get; set; }

        public decimal QtyReceived { get; set; }
        public string Status { get; set; }

        public decimal QtyGood { get; set; }
        public decimal QtyReject { get; set; }
        public decimal QtyRepair { get; set; }

        public decimal AllocationForRepair { get; set; }

        public string RepairWKRequired { get; set; }
        
        public int? LeadTimeDays { get; set; }
    }
}
