using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class History
    {
        public int Hid { get; set; }
        public int Uid { get; set; }
        public int? Runid { get; set; }
        public int OrigPoint { get; set; }
        public int ChanPoint { get; set; }
        public DateTime? Sdate { get; set; }
        public string Info { get; set; }
        public string Type { get; set; }
        public int Opponent { get; set; }
    }
}
