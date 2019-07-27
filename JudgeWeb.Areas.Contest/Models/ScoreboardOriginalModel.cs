using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class ScoreboardOriginalModel
    {
        public Team Team { get; set; }

        public TeamAffiliation Affil { get; set; }

        public RankCache Rank { get; set; }

        public IEnumerable<ScoreCache> Score { get; set; }
    }
}
