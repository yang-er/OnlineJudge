using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Features.Scoreboard
{
    public class BoardQuery
    {
        public Team Team { get; set; }

        public TeamAffiliation Affiliation { get; set; }

        public RankCache Rank { get; set; }

        public IEnumerable<ScoreCache> Score { get; set; }
    }
}
