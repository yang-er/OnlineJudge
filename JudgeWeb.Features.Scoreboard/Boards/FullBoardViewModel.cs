﻿using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Features.Scoreboard
{
    public class FullBoardViewModel : BoardViewModel
    {
        public DateTimeOffset UpdateTime { get; set; }

        public IEnumerable<Team> RankCache { get; set; }

        public IEnumerable<TeamAffiliation> Affiliations { get; set; }

        public IEnumerable<TeamCategory> Categories { get; set; }

        public bool IsPublic { get; set; }

        private Dictionary<int, TeamCategory> cats2;

        private Dictionary<int, TeamAffiliation> affs2;

        protected override IEnumerable<SortOrderModel> GetEnumerable()
        {
            cats2 = Categories.ToDictionary(t => t.CategoryId);
            affs2 = Affiliations.ToDictionary(t => t.AffiliationId);

            var rt = RankCache
                .Where(a => cats2.ContainsKey(a.CategoryId))
                .GroupBy(a => cats2[a.CategoryId].SortOrder)
                .OrderBy(g => g.Key);

            foreach (var g in rt)
            {
                var prob = new ProblemStatisticsModel[Problems.Length];
                for (int i = 0; i < Problems.Length; i++)
                    prob[i] = new ProblemStatisticsModel();
                yield return new SortOrderModel(GetViewModel(IsPublic, g, prob), prob);
            }
        }

        private IEnumerable<TeamModel> GetViewModel(
            bool ispublic,
            IEnumerable<Team> src,
            ProblemStatisticsModel[] stat)
        {
            int rank = 0;
            int last_rank = 0;
            int last_point = int.MinValue;
            int last_penalty = int.MinValue;
            var cats = new Dictionary<int, TeamCategory>();
            src = IRankingStrategy.SC[Contest.RankingStrategy].SortByRule(src, ispublic);

            foreach (var item in src)
            {
                int catid = item.CategoryId;
                string catName = null;
                if (!cats.Keys.Contains(catid))
                {
                    cats.Add(catid, cats2[catid]);
                    catName = cats2[catid].Name;
                }

                int point = ispublic ? item.RankCache.PointsPublic : item.RankCache.PointsRestricted;
                int penalty = ispublic ? item.RankCache.TotalTimePublic : item.RankCache.TotalTimeRestricted;
                rank++;
                if (last_point != point || last_penalty != penalty) last_rank = rank;
                last_point = point;
                last_penalty = penalty;

                var prob = new ScoreCellModel[Problems.Length];

                foreach (var pp in item.ScoreCache ?? Enumerable.Empty<ScoreCache>())
                {
                    var p = Problems.FirstOrDefault(a => a.ProblemId == pp.ProblemId);
                    if (p == null) continue;
                    var pid = p.Rank - 1;

                    if (ispublic)
                    {
                        prob[pid] = new ScoreCellModel
                        {
                            PendingCount = pp.PendingPublic,
                            IsFirstToSolve = pp.FirstToSolve,
                            JudgedCount = pp.SubmissionPublic,
                            Score = pp.ScorePublic,
                            SolveTime = pp.SolveTimePublic,
                        };
                    }
                    else
                    {
                        prob[pid] = new ScoreCellModel
                        {
                            PendingCount = pp.PendingRestricted,
                            IsFirstToSolve = pp.FirstToSolve,
                            JudgedCount = pp.SubmissionRestricted,
                            Score = pp.ScoreRestricted,
                            SolveTime = pp.SolveTimeRestricted,
                        };
                    }

                    if (prob[pid].Score.HasValue)
                    {
                        stat[pid].FirstSolve ??= prob[pid].Score;
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

                yield return new TeamModel
                {
                    ContestId = IsPublic ? default(int?) : item.ContestId,
                    TeamId = item.TeamId,
                    TeamName = item.TeamName,
                    Affiliation = affs2.GetValueOrDefault(item.AffiliationId)?.FormalName ?? "",
                    AffiliationId = affs2.GetValueOrDefault(item.AffiliationId)?.ExternalId ?? "null",
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
