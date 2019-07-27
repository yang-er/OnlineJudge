using System.Collections;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class ScoreboardSortModel : IEnumerable<TeamScoreModel>
    {
        readonly IEnumerable<TeamScoreModel> _inner;

        public ScoreboardProblemStatisticsModel[] Statistics { get; }

        public ScoreboardSortModel(
            IEnumerable<TeamScoreModel> items,
            ScoreboardProblemStatisticsModel[] stats)
        {
            _inner = items;
            Statistics = stats;
        }

        public IEnumerator<TeamScoreModel> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }
    }
}
