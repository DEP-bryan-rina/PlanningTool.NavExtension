using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Models
{
    public class ProcurementCenterModel
    {
        [Key]
        public int PK_ptProcID { get; set; }

        public int? _ptDocType { get; set; }
        public string? _ptDocNo { get; set; }
        public int? _ptLineNo { get; set; }

        public string? _ptHBLNo { get; set; }
        public string? _ptContainerNo { get; set; }
        public string? _ptVendorName { get; set; }

        public string? _ptItemCode { get; set; }
        public string? _ptItemDescription { get; set; }

        public decimal? _ptOutstandingQty { get; set; }
        public decimal? _ptPartialDelivered { get; set; }
        public decimal? _ptBalanceQty { get; set; }

        public DateTime? _ptContainerETA { get; set; }

        public string? _ptCreatedBy { get; set; }
        public DateTime? _ptCreatedDate { get; set; }

        public string? _ptModifiedBy { get; set; }
        public DateTime? _ptModifiedDate { get; set; }

        public string? _ptRemarks { get; set; }
        public int? _ptStatus { get; set; }        
    }
}
