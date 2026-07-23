namespace PlanningTool.NavExtension.Models
{
    public class ProcurementSubmitDto
    {
        public List<ProcurementCenterDto> TagItems { get; set; } = new();
        public List<UntagItemDto> UntagItems { get; set; } = new();
    } 

}
