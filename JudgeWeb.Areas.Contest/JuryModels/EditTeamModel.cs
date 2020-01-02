using System.ComponentModel;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryEditTeamModel
    {
        public int TeamId { get; set; }

        [DisplayName("Team name")]
        public string TeamName { get; set; }

        [DisplayName("Affiliation")]
        public int AffiliationId { get; set; }

        [DisplayName("Category")]
        public int CategoryId { get; set; }
    }
}
