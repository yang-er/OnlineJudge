using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Judge.Models
{
    public class ProblemEditModel
    {
        [DisplayName("Problem ID")]
        public int ProblemId { get; set; }

        [DisplayName("Problem Title")]
        public string Title { get; set; }

        [DisplayName("Time Limitation")]
        [Range(500, 15000)]
        public int TimeLimit { get; set; }

        [DisplayName("Memory Limitation")]
        [Range(32768, 1048576)]
        public int MemoryLimit { get; set; }

        [DisplayName("Original Source")]
        public string Source { get; set; }

        [DisplayName("Run Script")]
        public string RunScript { get; set; }

        [DisplayName("Compare Script")]
        public string CompareScript { get; set; }

        [DisplayName("Is this problem active?")]
        public bool IsActive { get; set; }

        public int Flag { get; set; }
    }
}
