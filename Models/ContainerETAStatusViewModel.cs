using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlanningTool.NavExtension.Models
{
    [Table("vw_ContainerETAStatus")]
    public class ContainerETAStatusViewModel
    {
        [Key]
        public int PK_ReminderID { get; set; }
        public int PermitID { get; set; }
        public string ReminderType { get; set; }
        public string RecipientEmail { get; set; }
        public DateTime? ContainerETA { get; set; }
        public string HBLNo { get; set; }
        public string ContainerNo { get; set; }
        public string PONo { get; set; }
        public string CountryOfOrigin { get; set; }
        public string EmailStatus { get; set; }
        public int EmailCount { get; set; }        
        public int ReminderStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SentDate { get; set; }

        public int? DaysToETA { get; set; }
        public int? DaysPastETA { get; set; }
        public string ETAStatus { get; set; }
    }
}
