namespace PlanningTool.NavExtension.Models
{
    public class PhysicalInventoryItem
    {
        public string? BinCode { get; set; }
        public string ItemNo { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ExtendedText { get; set; }
        public string? ColorCode { get; set; }
        public string? ColorForPC { get; set; }
        public string? LotNo { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? UoM { get; set; }

        public decimal QtyCalculated { get; set; }

        public string? CountTagNo { get; set; }

        public decimal? Counter1 { get; set; }
        public decimal? Counter2 { get; set; }
        public decimal? Counter3 { get; set; }
        public decimal? Counter4 { get; set; }
        public decimal? Counter5 { get; set; }
        public decimal? Counter6 { get; set; }
        public decimal? Counter7 { get; set; }
        public decimal? Counter8 { get; set; }
        public decimal? Counter9 { get; set; }
        public decimal? Counter10 { get; set; }
        public decimal? Counter11 { get; set; }
        public decimal? Counter12 { get; set; }
        public decimal? Counter13 { get; set; }
        public decimal? Counter14 { get; set; }
        public decimal? Counter15 { get; set; }
        public decimal? Counter16 { get; set; }
        public decimal? Counter17 { get; set; }
        public decimal? Counter18 { get; set; }
        public decimal? Counter19 { get; set; }
        public decimal? Counter20 { get; set; }

        public decimal CounterTotal { get; set; }
        public decimal Variance { get; set; }
    }
}