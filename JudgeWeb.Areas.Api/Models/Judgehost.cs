namespace JudgeWeb.Areas.Api.Models
{
    public class Judgehost
    {
        public string hostname { get; set; }
        public bool active { get; set; }
        public long polltime { get; set; }
        public string polltime_formatted { get; set; }
    }
}
