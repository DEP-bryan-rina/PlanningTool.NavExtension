using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlanningTool.NavExtension.Models
{
    public class ImPexCenterModel
    {
        [Key]
        public int PK_ptImpexID { get; set; }

        public string? _ptHBLNo { get; set; }           // ✅ FIX
        public string? _ptContainerNo { get; set; }     // ✅ FIX

        [DataType(DataType.Date)]
        public DateTime? _ptContainerETA { get; set; }

        public string? _ptRemarks { get; set; }         // ✅ FIX

        public string? _ptCreatedBy { get; set; }       // ✅ FIX
        public DateTime? _ptCreatedDate { get; set; }

        public string? _ptModifiedBy { get; set; }      // ✅ FIX
        public DateTime? _ptModifiedDate { get; set; }

        public int _ptStatus { get; set; }
    }

}
