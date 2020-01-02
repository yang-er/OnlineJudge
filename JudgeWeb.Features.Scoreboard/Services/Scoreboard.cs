using System.Collections.Generic;

namespace JudgeWeb.Features.Scoreboard
{
    public interface IScoreboard
    {
        IEnumerable<BoardQuery> SortByRule(IEnumerable<BoardQuery> src, bool isPublic);
    }
}
