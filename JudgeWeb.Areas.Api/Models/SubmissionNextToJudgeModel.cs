using System.Collections.Generic;

namespace JudgeWeb.Areas.Api.Models
{
    public class NextJudging
    {
        public int submitid { get; set; }
        public int cid { get; set; }
        public int teamid { get; set; }
        public int probid { get; set; }
        public string langid { get; set; }
        public int? rejudgingid { get; set; }
        public string entry_point { get; set; }
        public int? origsubmitid { get; set; }
        public double maxruntime { get; set; }
        public int memlimit { get; set; }
        public int outputlimit { get; set; }
        public string run { get; set; }
        public string compare { get; set; }
        public string compare_args { get; set; }
        public string compile_script { get; set; }
        public bool combined_run_compare { get; set; }
        public string compare_md5sum { get; set; }
        public string run_md5sum { get; set; }
        public string compile_script_md5sum { get; set; }
        public int judgingid { get; set; }
        public Dictionary<string, TestcaseToJudge> testcases { get; set; }
    }
}
