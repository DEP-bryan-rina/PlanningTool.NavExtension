using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Models
{
    public class vw_ProcurementETAEmailNotification
    {
        [Key]
        public int PK_ptProcID { get; set; }

        public string _ptDocNo { get; set; }
        public string _ptHBLNo { get; set; }
        public string _ptContainerNo { get; set; }

        public string _ptVendorName { get; set; }
        public string _ptItemDescription { get; set; }
        public string _ptCountryOfOrigin { get; set; }

        public DateTime? _ptContainerETA { get; set; }

        public decimal? _ptPartialDelivered { get; set; }

        public int DaysToETA { get; set; }

        public string ShipmentStatus { get; set; }


        // Email
        public int _PK_ptEmailNotification_ID { get; set; }

        public string _ptEmailAddress { get; set; }
        
    }
}
