namespace JudgeWeb.Areas.Api.Models
{
    public class ContestTeam
    {
        public string externalid { get; set; }
        public string[] group_ids { get; set; }
        public string affiliation { get; set; }
        public string id { get; set; }
        public string icpc_id { get; set; }
        public string name { get; set; }
        public string organization_id { get; set; }
        public string members { get; set; }
    }
}
