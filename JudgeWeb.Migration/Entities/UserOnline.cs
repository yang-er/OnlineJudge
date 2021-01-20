using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class UserOnline
    {
        public int Uid { get; set; }
        public DateTimeOffset LastTime { get; set; }
        public string LastIp { get; set; }
        public string Action { get; set; }
        public string Para { get; set; }
        public int LastRank { get; set; }
    }
}
