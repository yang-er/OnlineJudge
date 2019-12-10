namespace JudgeWeb.Areas.Polygon.Models
{
    public class JudgingDetailModel
    {
        public int ExecutionTime { get; set; }

        public Verdict Result { get; set; }

        public int TestcaseId { get; set; }
    }
}
