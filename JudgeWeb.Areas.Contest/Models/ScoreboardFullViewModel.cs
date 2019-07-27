using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Areas.Contest.Models
{
    public class ScoreboardFullViewModel : BoardViewModel
    {
        public IEnumerable<ScoreboardOriginalModel> RankCache { get; set; }

        public IEnumerable<TeamAffiliation> Affiliations { get; set; }

        public IEnumerable<TeamCategory> Categories { get; set; }

        public bool IsPublic { get; set; }

        private Dictionary<int, TeamCategory> cats2;

        protected override IEnumerable<ScoreboardSortModel> GetEnumerable()
        {
            cats2 = Categories.ToDictionary(t => t.CategoryId);

            var rt = RankCache
                .Where(a => cats2.ContainsKey(a.Team.CategoryId))
                .GroupBy(a => cats2[a.Team.CategoryId].SortOrder)
                .OrderBy(g => g.Key);

            foreach (var g in rt)
            {
                var prob = new ScoreboardProblemStatisticsModel[Problems.Length];
                for (int i = 0; i < Problems.Length; i++) prob[i] = new ScoreboardProblemStatisticsModel();
                yield return new ScoreboardSortModel(GetViewModel(IsPublic, g, prob), prob);
            }
        }

        private IEnumerable<TeamScoreModel> GetViewModel(
            bool ispublic,
            IEnumerable<ScoreboardOriginalModel> src,
            ScoreboardProblemStatisticsModel[] stat)
        {
            int rank = 0;
            int last_rank = 0;
            int last_point = int.MinValue;
            int last_penalty = int.MinValue;
            var cats = new Dictionary<int, TeamCategory>();

            if (ispublic)
            {
                src = src.OrderByDescending(a => a.Rank.PointsPublic)
                    .ThenBy(a => a.Rank.TotalTimePublic);
            }
            else
            {
                src = src.OrderByDescending(a => a.Rank.PointsRestricted)
                    .ThenBy(a => a.Rank.TotalTimeRestricted);
            }

            foreach (var item in src)
            {
                int catid = item.Team.CategoryId;
                string catName = null;
                if (!cats.Keys.Contains(catid))
                {
                    cats.Add(catid, cats2[catid]);
                    catName = cats2[catid].Name;
                }

                int point = ispublic ? item.Rank.PointsPublic : item.Rank.PointsRestricted;
                int penalty = ispublic ? item.Rank.TotalTimePublic : item.Rank.TotalTimeRestricted;
                rank++;
                if (last_point != point || last_penalty != penalty) last_rank = rank;
                last_point = point;
                last_penalty = penalty;

                var prob = new ScoreboardCellModel[Problems.Length];

                foreach (var pp in item.Score ?? Enumerable.Empty<ScoreCache>())
                {
                    var p = Problems.FirstOrDefault(a => a.ProblemId == pp.ProblemId);
                    if (p == null) continue;
                    var pid = p.Rank - 1;

                    if (ispublic)
                    {
                        prob[pid] = new ScoreboardCellModel
                        {
                            PendingCount = pp.PendingPublic,
                            IsFirstToSolve = pp.FirstToSolve,
                            JudgedCount = pp.SubmissionPublic,
                            SolveTime = pp.IsCorrectPublic ? (int)pp.SolveTimePublic / 60 : default(int?),
                        };
                    }
                    else
                    {
                        prob[pid] = new ScoreboardCellModel
                        {
                            PendingCount = pp.PendingRestricted,
                            IsFirstToSolve = pp.FirstToSolve,
                            JudgedCount = pp.SubmissionRestricted,
                            SolveTime = pp.IsCorrectRestricted ? (int)pp.SolveTimeRestricted / 60 : default(int?),
                        };
                    }

                    if (prob[pid].SolveTime.HasValue)
                    {
                        stat[pid].FirstSolve = Math.Min(stat[pid].FirstSolve ?? int.MaxValue, prob[pid].SolveTime.Value);
                        stat[pid].Accepted++;
                        stat[pid].Rejected += prob[pid].JudgedCount - 1;
                        stat[pid].Pending += prob[pid].PendingCount;
                    }
                    else
                    {
                        stat[pid].Rejected += prob[pid].JudgedCount;
                        stat[pid].Pending += prob[pid].PendingCount;
                    }
                }

                yield return new TeamScoreModel
                {
                    TeamId = item.Team.TeamId,
                    TeamName = item.Team.TeamName,
                    Affiliation = item.Affil.FormalName,
                    AffiliationId = item.Affil.ExternalId,
                    Category = catName,
                    CategoryColor = cats[catid].Color,
                    Points = point,
                    Penalty = penalty,
                    Rank = last_rank,
                    ShowRank = last_rank == rank,
                    Problems = prob,
                };
            }
        }
    }
}
