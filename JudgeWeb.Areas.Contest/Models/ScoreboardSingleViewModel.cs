using JudgeWeb.Data;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Areas.Contest.Models
{
    public class ScoreboardSingleViewModel : BoardViewModel
    {
        public Team Team { get; set; }

        public TeamAffiliation Affiliation { get; set; }

        public TeamCategory Category { get; set; }

        public RankCache Rank { get; set; }

        public IEnumerable<ScoreCache> Score { get; set; }

        protected override IEnumerable<ScoreboardSortModel> GetEnumerable()
        {
            yield return new ScoreboardSortModel(GetSingleScore(), null);
        }

        private IEnumerable<TeamScoreModel> GetSingleScore()
        {
            var prob = new ScoreboardCellModel[Problems.Length];

            foreach (var pp in Score ?? Enumerable.Empty<ScoreCache>())
            {
                var p = Problems.FirstOrDefault(a => a.ProblemId == pp.ProblemId);
                if (p == null) continue;
                var pid = p.Rank - 1;

                prob[pid] = new ScoreboardCellModel
                {
                    PendingCount = pp.PendingRestricted,
                    IsFirstToSolve = pp.FirstToSolve,
                    JudgedCount = pp.SubmissionRestricted,
                    SolveTime = pp.IsCorrectRestricted ? (int)pp.SolveTimeRestricted / 60 : default(int?),
                };
            }

            yield return new TeamScoreModel
            {
                TeamId = Team.TeamId,
                TeamName = Team.TeamName,
                Affiliation = Affiliation.FormalName,
                AffiliationId = Affiliation.ExternalId,
                Category = Category.Name,
                CategoryColor = Category.Color,
                Points = Rank?.PointsRestricted ?? 0,
                Penalty = Rank?.TotalTimeRestricted ?? 0,
                ShowRank = true,
                Problems = prob,
            };
        }

        public List<TeamViewSubmissionModel> Submissions { get; set; }

        public IEnumerable<Clarification> ReceivedClarifications { get; set; }

        public IEnumerable<Clarification> RequestedClarifications { get; set; }

        public string MessageInfo { get; set; }
    }
}
