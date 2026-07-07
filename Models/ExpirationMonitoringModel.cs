using Microsoft.EntityFrameworkCore;

namespace PlanningTool.NavExtension.Models
{
    [Keyless]
    public class ExpirationMonitoringModel
    {
        public string LocationCode { get; set; }
        public string ZoneCode { get; set; }
        public string BinCode { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal QtyBase { get; set; }
        public string VariantCode { get; set; }
        public decimal QtyPerUnitOfMeasure { get; set; }
        public string UnitOfMeasureCode { get; set; }
        public string LotNo { get; set; }
        public DateTime ExpirationDate { get; set; }

        public int DaysRemaining
        {
            get
            {
                return (ExpirationDate.Date - DateTime.Now.Date).Days;
            }
        }
    }
}
