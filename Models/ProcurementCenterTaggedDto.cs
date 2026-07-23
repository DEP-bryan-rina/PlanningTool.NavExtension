using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Models
{
    public class ProcurementCenterTaggedDto
    {
        // Declare column from Table Purchase Line
        [Key]
        public int?  IDDocType { get; set; }
        public string? IDDocNo { get; set; } = string.Empty;
        public int? IDLineNo { get; set; }

        public string? VendorName { get; set; }

        public string? ItemCode { get; set; }

        public string? ItemDescription { get; set; }

        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? OutstandingQty { get; set; }


        // Declare column from Table Procurement Center
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? ptBalanceQty { get; set; }

        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? ptPartialDelivered { get; set; }
        public string? ptHBLNo { get; set; }
        public string? ptContainerNo { get; set; }     
        
        public DateTime? ptContainerETA { get; set; }

        public string? ptCountryOfOrigin { get; set; }

        public string? ptRemarks { get; set; }

    }
}
