using Microsoft.EntityFrameworkCore;

namespace PlanningTool.NavExtension.Models
{
    [Keyless]
    public class TagProcurementRequest
    {
        public string HBLNo { get; set; }
        public string ContainerNo { get; set; }

        public int DocType { get; set; }

        public string DocNo { get; set; }
        public int LineNo { get; set; }

        public string ItemCode { get; set; }
        public string Description { get; set; }

        public decimal PartialDelivered { get; set; }

        public DateTime? CETA { get; set; }

        public string? Remarks { get; set; }
    }
}
