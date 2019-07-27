using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryViewTeamModel
    {
        public Team Team => TeamScoreboard.Team;

        public TeamAffiliation Affiliation => TeamScoreboard.Affiliation;

        public TeamCategory Category => TeamScoreboard.Category;

        public ScoreboardSingleViewModel TeamScoreboard { get; set; }

        public IEnumerable<JuryListSubmissionModel> Submissions { get; set; }
    }
}
