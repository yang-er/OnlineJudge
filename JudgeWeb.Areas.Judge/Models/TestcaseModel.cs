using System.ComponentModel;

namespace JudgeWeb.Areas.Judge.Models
{
    public class ProblemTestcaseModel
    {
        [DisplayName("Secret data, not shown to public")]
        public bool IsSecret { get; set; }

        public int TestcaseId { get; set; }

        public int ProblemId { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        [DisplayName("Input Content")]
        public string InputContent { get; set; }

        [DisplayName("Output Content")]
        public string OutputContent { get; set; }
    }
}
