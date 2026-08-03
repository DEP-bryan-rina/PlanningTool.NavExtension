using System.ComponentModel.DataAnnotations;

namespace PlanningTool.NavExtension.Models
{
    public class EmailNotication
    {
        [Key]

        public int _PK_ptEmailNotification_ID { get; set; }

        public string _ptEmailAddress { get; set; }

        public bool _ptImPex { get; set; }

        public bool _ptProcurement { get; set; }

        public bool _pt2WeeksReminder { get; set; }
        public string _pt2WeeksTrigger { get; set; }

        public bool _ptDelaysReminder { get; set; }
        public string _ptDelaysTrigger { get; set; }

        public string _ptUserType { get; set; }

        public string _ptStatus { get; set; }


        public DateTime? _ptCreatedDate { get; set; }
        public DateTime? _ptUpdatedDate { get; set; }        

    }
}
