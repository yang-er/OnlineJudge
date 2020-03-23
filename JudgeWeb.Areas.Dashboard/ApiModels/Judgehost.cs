using JudgeWeb.Data;

namespace JudgeWeb.Areas.Api.Models
{
    public class Judgehost
    {
        public string hostname { get; set; }
        public bool active { get; set; }
        public long polltime { get; set; }
        public string polltime_formatted { get; set; }

        public Judgehost(JudgeHost a)
        {
            hostname = a.ServerName;
            active = a.Active;
            polltime = a.PollTime.ToUnixTimeSeconds();
            polltime_formatted = a.PollTime.ToString("yyyy-MM-dd HH:mm:ss zzz");
        }
    }
}
