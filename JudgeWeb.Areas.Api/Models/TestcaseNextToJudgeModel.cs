using Newtonsoft.Json;

namespace JudgeWeb.Areas.Api.Models
{
    public class TestcaseNextToJudgeModel
    {
        [JsonProperty("testcaseid")]
        public int TestcaseId { get; set; }

        [JsonProperty("md5sum_input")]
        public string Md5sumInput { get; set; }

        [JsonProperty("md5sum_output")]
        public string Md5sumOutput { get; set; }

        [JsonProperty("probid")]
        public int ProblemId { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("description_as_string")]
        public string Description { get; set; }
    }
}
