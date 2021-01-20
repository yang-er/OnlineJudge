using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Balloon
    {
        public int Bid { get; set; }
        public int Cid { get; set; }
        public int Uid { get; set; }
        public int Pid { get; set; }
        public string Color { get; set; }
        public int Submitid { get; set; }
        public byte Status { get; set; }
    }
}
