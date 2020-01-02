using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Features.Scoreboard
{
    public class ICPCScoreboard : IScoreboard
    {
        public IEnumerable<BoardQuery> SortByRule(IEnumerable<BoardQuery> src, bool isPublic)
        {
            if (isPublic)
            {
                return src.OrderByDescending(a => a.Rank.PointsPublic)
                    .ThenBy(a => a.Rank.TotalTimePublic);
            }
            else
            {
                return src.OrderByDescending(a => a.Rank.PointsRestricted)
                    .ThenBy(a => a.Rank.TotalTimeRestricted);
            }
        }
    }
}
