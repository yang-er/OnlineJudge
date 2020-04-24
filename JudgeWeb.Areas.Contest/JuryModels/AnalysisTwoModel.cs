using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Models
{
    public class AnalysisTwoModel
    {
        public int TotalMinutes { get; }

        public int TotalSubmissions { get; private set; }

        public int TotalAccepted { get; private set; }

        public int TeamAttempted { get; private set; }

        public int TeamAccepted { get; private set; }

        public int[,] VerdictStatistics { get; }

        public IEnumerable List { get; private set; }

        public ContestProblem Problem { get; }

        private AnalysisTwoModel(int time, ContestProblem cp)
        {
            TotalMinutes = time;
            VerdictStatistics = new int[12, time + 1];
            Problem = cp;
        }

        private static readonly IReadOnlyDictionary<Verdict, string> Colors
            = new Dictionary<Verdict, string>
            {
                [Verdict.Accepted] = "#01df01",
                [Verdict.WrongAnswer] = "red",
                [Verdict.TimeLimitExceeded] = "orange",
                [Verdict.RuntimeError] = "#ff3399",
                [Verdict.MemoryLimitExceeded] = "purple",
                [Verdict.CompileError] = "grey"
            };

        public static async Task<AnalysisTwoModel> AnalysisAsync(
            ISubmissionStore store,
            Data.Contest contest,
            ContestProblem prob,
            IReadOnlyDictionary<int, (string, string)> cls)
        {
            int cid = contest.ContestId;
            var startTime = contest.StartTime.Value;
            var endTime = contest.EndTime.Value;
            int pid = prob.ProblemId;

            var result = await store.ListWithJudgingAsync(
                predicate: s => s.ContestId == cid && s.Time >= startTime && s.Time <= endTime && s.ProblemId == pid,
                selector: (s, j) => new { s.Time, s.SubmissionId, j.Status, s.Author, s.Language, j.JudgingId, j.ExecuteTime });

            var tof = (int)Math.Ceiling((endTime - startTime).TotalMinutes);
            var model = new AnalysisTwoModel(tof, prob);
            var dbl = model.VerdictStatistics;
            int toc = 0, toac = 0;
            var set1 = new HashSet<int>();
            var set2 = new HashSet<int>();

            foreach (var stat in result)
            {
                if (!cls.ContainsKey(stat.Author)) continue;
                toc++;
                int thisTime = (int)Math.Ceiling((stat.Time - startTime).TotalMinutes);
                set1.Add(stat.Author);
                dbl[(int)stat.Status, thisTime]++;

                if (stat.Status == Verdict.Accepted)
                {
                    toac++;
                    set2.Add(stat.Author);
                }
            }

            for (int i = 0; i < 12; i++)
                for (int j = 1; j <= tof; j++)
                    dbl[i, j] += dbl[i, j - 1];

            model.TotalSubmissions = toc;
            model.TotalAccepted = toac;
            model.TeamAccepted = set2.Count;
            model.TeamAttempted = set1.Count;
            
            model.List = result
                .Where(a => a.ExecuteTime.HasValue && cls.ContainsKey(a.Author))
                .OrderBy(a => a.ExecuteTime.Value)
                .Select(a => new
                {
                    id = a.SubmissionId,
                    label = $"j{a.JudgingId}",
                    value = a.ExecuteTime / 1000.0,
                    team = cls.GetValueOrDefault(a.Author).Item1 ?? "undefined",
                    submittime = a.Time - startTime,
                    color = Colors.GetValueOrDefault(a.Status),
                });
            
            return model;
        }
    }
}
