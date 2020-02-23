using JudgeWeb.Features.Scoreboard;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Contest.Models
{
    public class ScoreboardDataModel
    {
        public Dictionary<int, BoardQuery> Data { get; set; }

        public DateTimeOffset RefreshTime { get; set; }

        public Dictionary<int, int> Statistics { get; set; }
    }
}
