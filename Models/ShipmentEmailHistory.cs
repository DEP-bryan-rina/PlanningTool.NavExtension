using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Models
{
    public class ShipmentEmailHistory
    {
        [Key]
        public int _PK_HistoryID { get; set; }

        public int _ptProcID { get; set; }

        public int _ptEmailNotificationID { get; set; }

        public string _ptReminderType { get; set; } = string.Empty;

        public DateTime? _ptLastSentDate { get; set; }

        public DateTime? _ptCreatedDate { get; set; }
    }
}