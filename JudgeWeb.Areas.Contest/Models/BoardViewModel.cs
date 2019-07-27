using JudgeWeb.Data;
using System.Collections;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public abstract class BoardViewModel : IEnumerable<ScoreboardSortModel>
    {
        public Data.Contest Contest { get; set; }

        public HashSet<(string, string)> ShowCategory { get; set; }

        public ContestProblem[] Problems { get; set; }

        protected abstract IEnumerable<ScoreboardSortModel> GetEnumerable();

        public IEnumerator<ScoreboardSortModel> GetEnumerator()
        {
            ShowCategory = new HashSet<(string, string)>();
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
