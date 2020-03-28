using JudgeWeb.Data;

namespace JudgeWeb.Domains.Contests.ApiModels
{
    [EntityType("organizations")]
    public class Organization : EventEntity
    {
        public string icpc_id { get; set; }
        public string shortname { get; set; }
        public string country { get; set; }
        public string name { get; set; }
        public string formal_name { get; set; }

        public Organization() { }

        public Organization(TeamAffiliation a)
        {
            id = a.ExternalId;
            name = a.ExternalId.ToUpper();
            shortname = a.ExternalId.ToUpper();
            country = a.CountryCode;
            formal_name = a.FormalName;
            icpc_id = id;
        }
    }
}
