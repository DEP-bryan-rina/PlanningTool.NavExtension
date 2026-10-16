namespace PlanningTool.NavExtension.Models
{
    public class QARepairMRPDetail
    {
        public string? SalesOrderNo { get; set; }

        public string? CustomerNo { get; set; }

        public DateTime? ShipmentDate { get; set; }

        public decimal Quantity { get; set; }

        public string? SalesProductCode { get; set; }

        public string? ItemCode { get; set; }

        public decimal AllocationForRepair { get; set; }

        public string? RequiredWeek { get; set; }    // NEW
    }
}