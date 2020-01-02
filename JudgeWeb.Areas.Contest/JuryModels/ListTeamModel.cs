using System;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryListTeamModel
    {
        public int TeamId { get; set; }

        public string TeamName { get; set; }

        public string UserName { get; set; }

        public string Category { get; set; }

        public string Affiliation { get; set; }

        public string AffiliationName { get; set; }

        public int Status { get; set; }

        public DateTimeOffset? RegisterTime { get; set; }
    }
}
