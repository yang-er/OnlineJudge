using System.ComponentModel;

namespace JudgeWeb.Areas.Misc.Models
{
    public class CodeSubmitModel
    {
        [DisplayName("Your Code")]
        public string Code { get; set; }

        [DisplayName("Language")]
        public int Language { get; set; }

        public int ProblemId { get; set; }
    }
}
