namespace JudgeWeb.Domains.Contests.ApiModels
{
    [EntityType("teams")]
    public class Team : EventEntity
    {
        public string externalid { get; set; }
        public string[] group_ids { get; set; }
        public string affiliation { get; set; }
        public string icpc_id { get; set; }
        public string name { get; set; }
        public string organization_id { get; set; }
        public string members { get; set; }

        public Team() { }

        public Team(Data.Team t, Data.TeamAffiliation a)
        {
            group_ids = new[] { $"{t.CategoryId}" };
            organization_id = a.ExternalId;
            id = $"{t.TeamId}";
            name = t.TeamName;
            externalid = $"team{t.TeamId}";
            icpc_id = $"team{t.TeamId}";
            affiliation = a.FormalName;
        }
    }
}
