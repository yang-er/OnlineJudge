using System;

namespace JudgeWeb.Data.Api
{
    [EntityType("contests")]
    public class ContestInfo : ContestEventEntity
    {
        public string formal_name { get; set; }
        public int penalty_time { get; set; }
        public DateTimeOffset start_time { get; set; }
        public DateTimeOffset end_time { get; set; }
        public TimeSpan duration { get; set; }
        public TimeSpan? scoreboard_freeze_duration { get; set; }
        public string name { get; set; }
        public string shortname { get; set; }

        public ContestInfo() { }

        public ContestInfo(Contest c)
        {
            formal_name = c.Name ?? "";
            name = c.Name ?? "";
            shortname = c.ShortName ?? "";
            id = $"{c.ContestId}";
            penalty_time = 20;
            start_time = c.StartTime ?? new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.FromHours(8));
            end_time = c.EndTime ?? new DateTimeOffset(2050, 1, 1, 5, 0, 0, TimeSpan.FromHours(8));
            duration = end_time - start_time;
            scoreboard_freeze_duration = c.EndTime - c.FreezeTime;
        }
    }
}
