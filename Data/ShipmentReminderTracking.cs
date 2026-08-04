using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Data
{
    public class ShipmentReminderTracking
    {
        [Key]
        public int PK_ptShipmentReminderTrackingID { get; set; }

        public int _ptProcID { get; set; }

        public int _ptEmailNotificationID { get; set; }

        public string _ptReminderType { get; set; } = "";

        public DateTime? _ptFirstTriggerDate { get; set; }

        public DateTime? _ptLastSentDate { get; set; }

        public int _ptSendCount { get; set; }

        public string _ptStatus { get; set; } = "";

        public DateTime _ptCreatedDate { get; set; }

        public DateTime? _ptUpdatedDate { get; set; }
    }
}