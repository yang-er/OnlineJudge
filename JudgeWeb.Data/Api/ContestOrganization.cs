namespace JudgeWeb.Data.Api
{
    [EntityType("organizations")]
    public class ContestOrganization : ContestEventEntity
    {
        public string icpc_id { get; set; }
        public string shortname { get; set; }
        public string country { get; set; }
        public string name { get; set; }
        public string formal_name { get; set; }

        public ContestOrganization() { }

        public ContestOrganization(TeamAffiliation a)
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
