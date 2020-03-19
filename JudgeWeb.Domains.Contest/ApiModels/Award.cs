namespace JudgeWeb.Domains.Contests.ApiModels
{
    [EntityType("awards")]
    public class Award : EventEntity
    {
        public string citation { get; set; }
        public string[] team_ids { get; set; }
    }
}
