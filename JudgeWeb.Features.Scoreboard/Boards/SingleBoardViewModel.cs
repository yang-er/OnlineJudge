using JudgeWeb.Data;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Features.Scoreboard
{
    public class SingleBoardViewModel : BoardViewModel
    {
        public BoardQuery QueryInfo { get; set; }

        public TeamCategory Category { get; set; }

        protected override IEnumerable<SortOrderModel> GetEnumerable()
        {
            yield return new SortOrderModel(GetSingleScore(), null);
        }

        private IEnumerable<TeamModel> GetSingleScore()
        {
            var prob = new ScoreCellModel[Problems.Length];

            foreach (var pp in QueryInfo.Score ?? Enumerable.Empty<ScoreCache>())
            {
                var p = Problems.FirstOrDefault(a => a.ProblemId == pp.ProblemId);
                if (p == null) continue;
                var pid = p.Rank - 1;

                prob[pid] = new ScoreCellModel
                {
                    PendingCount = pp.PendingRestricted,
                    IsFirstToSolve = pp.FirstToSolve,
                    JudgedCount = pp.SubmissionRestricted,
                    SolveTime = pp.IsCorrectRestricted ? (int)pp.SolveTimeRestricted / 60 : default(int?),
                };
            }

            yield return new TeamModel
            {
                TeamId = QueryInfo.Team.TeamId,
                TeamName = QueryInfo.Team.TeamName,
                Affiliation = QueryInfo.Affiliation.FormalName,
                AffiliationId = QueryInfo.Affiliation.ExternalId,
                Category = Category.Name,
                CategoryColor = Category.Color,
                Points = QueryInfo.Rank?.PointsRestricted ?? 0,
                Penalty = QueryInfo.Rank?.TotalTimeRestricted ?? 0,
                ShowRank = true,
                Problems = prob,
            };
        }
    }
}
