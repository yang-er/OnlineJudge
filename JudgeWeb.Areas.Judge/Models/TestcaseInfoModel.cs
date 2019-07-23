namespace JudgeWeb.Areas.Judge.Models
{
    public class TestcaseInfoModel
    {
        public int TestcaseId { get; set; }

        public int ProblemId { get; set; }

        public int Rank { get; set; }

        public bool IsSecret { get; set; }

        public string Description { get; set; }

        public long InputLength { get; set; }

        public long OutputLength { get; set; }
    }
}
