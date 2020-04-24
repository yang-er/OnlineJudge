using JudgeWeb.Domains.Problems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Models
{
    public class AnalysisOneModel
    {
        readonly Dictionary<string, int> acc_lang = new Dictionary<string, int>();
        readonly Dictionary<string, int> rej_lang = new Dictionary<string, int>();
        readonly Dictionary<int, int> acc_prob = new Dictionary<int, int>();
        readonly Dictionary<int, int> rej_prob = new Dictionary<int, int>();
        readonly Dictionary<int, int> last_s = new Dictionary<int, int>();
        readonly Dictionary<(int team, int prob), (int ac, int at)> team = new Dictionary<(int, int), (int, int)>();

        public IReadOnlyDictionary<string, int> AcceptedLanguages => acc_lang;

        public IReadOnlyDictionary<string, int> AttemptedLanguages => rej_lang;

        public IReadOnlyDictionary<int, int> AcceptedProblems => acc_prob;

        public IReadOnlyDictionary<int, int> AttemptedProblems => rej_prob;

        public IReadOnlyDictionary<int, int> TeamLastSubmission => last_s;

        public IReadOnlyDictionary<(int team, int prob), (int ac, int at)> TeamStatistics => team;

        public int TotalMinutes { get; }

        public int TotalSubmissions { get; private set; }

        public int[,] VerdictStatistics { get; }

        private AnalysisOneModel(int time)
        {
            TotalMinutes = time;
            VerdictStatistics = new int[12, time + 1];
        }

        private static void Add<T>(Dictionary<T, int> kvp, T key)
        {
            kvp[key] = kvp.GetValueOrDefault(key) + 1;
        }

        public static async Task<AnalysisOneModel> AnalysisAsync(
            ISubmissionStore store,
            Data.Contest contest,
            IReadOnlyDictionary<int, (string, string)> cls)
        {
            int cid = contest.ContestId;
            var startTime = contest.StartTime.Value;
            var endTime = contest.EndTime.Value;

            var result = await store.ListWithJudgingAsync(
                predicate: s => s.ContestId == cid && s.Time >= startTime && s.Time <= endTime,
                selector: (s, j) => new { s.Time, j.Status, s.ProblemId, s.Author, s.Language });

            var tof = (int)Math.Ceiling((endTime - startTime).TotalMinutes);
            var model = new AnalysisOneModel(tof);
            var dbl = model.VerdictStatistics;
            int toc = 0;

            foreach (var stat in result)
            {
                if (!cls.ContainsKey(stat.Author)) continue;
                toc++;
                Add(model.rej_lang, stat.Language);
                Add(model.rej_prob, stat.ProblemId);
                int thisTime = (int)Math.Ceiling((stat.Time - startTime).TotalMinutes);

                dbl[(int)stat.Status, thisTime]++;
                var keyid = (stat.Author, stat.ProblemId);
                var valv = model.team.GetValueOrDefault(keyid);
                valv = (valv.ac, valv.at + 1);

                if (stat.Status == Verdict.Accepted)
                {
                    Add(model.acc_lang, stat.Language);
                    Add(model.acc_prob, stat.ProblemId);
                    model.last_s[stat.Author] = Math.Max(thisTime, model.last_s.GetValueOrDefault(stat.Author));
                    valv = (valv.ac + 1, valv.at);
                }

                model.team[keyid] = valv;
            }

            foreach (var langid in model.acc_lang.Keys.Union(model.rej_lang.Keys).ToHashSet())
            {
                model.acc_lang[langid] = model.acc_lang.GetValueOrDefault(langid);
                model.rej_lang[langid] = model.rej_lang.GetValueOrDefault(langid);
            }

            for (int i = 0; i < 12; i++)
                for (int j = 1; j <= tof; j++)
                    dbl[i, j] += dbl[i, j - 1];

            model.TotalSubmissions = toc;
            return model;
        }
    }
}
