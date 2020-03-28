using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Domains.Contests
{
    public class ScoreboardDataModel
    {
        public Dictionary<int, Team> Data { get; set; }

        public DateTimeOffset RefreshTime { get; set; }

        public Dictionary<int, int> Statistics { get; set; }
    }
}
