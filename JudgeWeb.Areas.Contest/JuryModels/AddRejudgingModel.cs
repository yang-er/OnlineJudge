using System;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Contest.Models
{
    public class AddRejudgingModel
    {
        [Required]
        public string Reason { get; set; }

        public string[] Judgehosts { get; set; }

        public Verdict[] Verdicts { get; set; }

        public int[] Problems { get; set; }

        public string[] Languages { get; set; }

        public int[] Teams { get; set; }

        public int? Submission { get; set; }

        [TimeSpan]
        public string TimeBefore { get; set; }

        [TimeSpan]
        public string TimeAfter { get; set; }
    }
}
