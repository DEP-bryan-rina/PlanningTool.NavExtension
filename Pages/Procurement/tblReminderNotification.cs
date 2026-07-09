using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Pages.Procurement
{
    internal class tblReminderNotification : ReminderNotification
    {
        public int PermitID { get; set; }
        public string ReminderType { get; set; }
        public string EmailStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}