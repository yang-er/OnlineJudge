using System.ComponentModel;

namespace JudgeWeb.Areas.Contest.Models
{
    public class AddTeamByGroupModel
    {
        [DisplayName("Class")]
        public int GroupId { get; set; }

        [DisplayName("Only temporary users are added")]
        public bool AddNonTemporaryUser { get; set; }

        [DisplayName("Affiliation")]
        public int AffiliationId { get; set; }

        [DisplayName("Category")]
        public int CategoryId { get; set; }
    }
}
