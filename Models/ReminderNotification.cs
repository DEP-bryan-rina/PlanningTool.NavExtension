using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Models
{
    public class ReminderNotification
    {
        [Key]
        public int PK_ReminderID { get; set; }
        public int PermitID { get; set; }
        public string PONo { get; set; }
        public string? HBLNo { get; set; }
        public string? ContainerNo { get; set; }        
        public string ReminderType { get; set; }
        public string RecipientEmail { get; set; }
        public DateTime? ContainerETA { get; set; }
        public string EmailStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SentDate { get; set; }

    }
}
