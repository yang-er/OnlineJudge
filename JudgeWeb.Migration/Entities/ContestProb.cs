using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class ContestProb
    {
        public int Id { get; set; }
        public int Cid { get; set; }
        public int Pid { get; set; }
        public string Seq { get; set; }
        public string Color { get; set; }
    }
}
