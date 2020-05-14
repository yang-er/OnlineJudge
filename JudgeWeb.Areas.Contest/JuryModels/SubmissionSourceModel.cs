using JudgeWeb.Data;

namespace JudgeWeb.Areas.Contest.Models
{
    public class SubmissionSourceModel
    {
        public int ProblemId { get; set; }

        public int TeamId { get; set; }

        public string NewCode { get; set; }

        public int NewId { get; set; }

        public Language NewLang { get; set; }

        public string OldCode { get; set; }

        public int? OldId { get; set; }

        public Language OldLang { get; set; }
    }
}
