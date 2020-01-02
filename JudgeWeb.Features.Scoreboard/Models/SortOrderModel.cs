using System.Collections;
using System.Collections.Generic;

namespace JudgeWeb.Features.Scoreboard
{
    public class SortOrderModel : IEnumerable<TeamModel>
    {
        readonly IEnumerable<TeamModel> _inner;

        public ProblemStatisticsModel[] Statistics { get; }

        public SortOrderModel(
            IEnumerable<TeamModel> items,
            ProblemStatisticsModel[] stats)
        {
            _inner = items;
            Statistics = stats;
        }

        public IEnumerator<TeamModel> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }
    }
}
