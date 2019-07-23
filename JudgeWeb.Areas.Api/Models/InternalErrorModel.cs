using Newtonsoft.Json;

namespace JudgeWeb.Areas.Api.Models
{
    public class InternalErrorModel
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("judgehostlog")]
        public string JudgehostLog { get; set; }

        [JsonProperty("disabled")]
        public string Disabled { get; set; }

        [JsonProperty("cid")]
        public int? ContestId { get; set; }

        [JsonProperty("judgingid")]
        public int? JudgingId { get; set; }
    }
}
