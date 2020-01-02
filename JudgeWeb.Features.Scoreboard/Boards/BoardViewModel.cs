using JudgeWeb.Data;
using System.Collections;
using System.Collections.Generic;

namespace JudgeWeb.Features.Scoreboard
{
    public abstract class BoardViewModel : IEnumerable<SortOrderModel>
    {
        public Contest Contest { get; set; }

        public HashSet<(string, string)> ShowCategory { get; private set; }

        public ContestProblem[] Problems { get; set; }

        public IScoreboard ExecutionStrategy { get; set; }

        protected abstract IEnumerable<SortOrderModel> GetEnumerable();

        public IEnumerator<SortOrderModel> GetEnumerator()
        {
            ShowCategory = new HashSet<(string, string)>();
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
