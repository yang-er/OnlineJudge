namespace JudgeWeb.Data.Api
{
    [EntityType("awards")]
    public class ContestAward : ContestEventEntity
    {
        public string citation { get; set; }
        public string[] team_ids { get; set; }
    }
}
