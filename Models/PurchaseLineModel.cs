using Microsoft.EntityFrameworkCore;

namespace PlanningTool.NavExtension.Models
{
    [Keyless]
    public class PurchaseLineModel
    {
        public int IDDocType { get; set; }

        public string IDDocNo { get; set; }

        public int IDLineNo { get; set; }

        public string VendorName { get; set; }

        public string ItemCode { get; set; }

        public string ItemDescription { get; set; }

        public decimal OutstandingQty { get; set; }
    }
}
