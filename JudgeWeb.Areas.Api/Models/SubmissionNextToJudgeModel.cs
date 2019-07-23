using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JudgeWeb.Areas.Api.Models
{
    public class SubmissionNextToJudgeModel
    {
        [JsonProperty("submitid")]
        public int SubmitId { get; set; }

        [JsonProperty("cid")]
        public int ContestId { get; set; }

        [JsonProperty("teamid")]
        public int TeamId { get; set; }

        [JsonProperty("probid")]
        public int ProblemId { get; set; }

        [JsonProperty("langid")]
        public string LanguageId { get; set; }

        [JsonProperty("rejudgingid")]
        public int? RejudgingId { get; set; }

        [JsonProperty("entry_point")]
        public string EntryPoint { get; set; }

        [JsonProperty("origsubmitid")]
        public int? OrigSubmitId { get; set; }

        [JsonProperty("maxruntime")]
        public double MaxRunTime { get; set; }

        [JsonProperty("memlimit")]
        public int MemLimit { get; set; }

        [JsonProperty("outputlimit")]
        public int OutputLimit { get; set; }

        [JsonProperty("run")]
        public string Run { get; set; }

        [JsonProperty("compare")]
        public string Compare { get; set; }

        [JsonProperty("compare_args")]
        public string CompareArgs { get; set; }

        [JsonProperty("compile_script")]
        public string CompileScript { get; set; }

        [JsonProperty("combined_run_compare")]
        public bool CombinedRunCompare { get; set; }

        [JsonProperty("compare_md5sum")]
        public string CompareMd5sum { get; set; }

        [JsonProperty("run_md5sum")]
        public string RunMd5sum { get; set; }

        [JsonProperty("compile_script_md5sum")]
        public string CompileScriptMd5sum { get; set; }

        [JsonProperty("judgingid")]
        public int JudgingId { get; set; }

        [JsonProperty("testcases")]
        public JObject Testcases { get; set; }
    }
}
