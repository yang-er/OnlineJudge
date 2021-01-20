using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class SubmitNow
    {
        public int Sid { get; set; }
        public int Uid { get; set; }
        public string Userid { get; set; }
        public int Pid { get; set; }
        public DateTime Sdate { get; set; }
        public string Lang { get; set; }
        public string Source { get; set; }
        public string JudgeStat { get; set; }
        public int Cid { get; set; }
        public byte? Priority { get; set; }
        public byte Status { get; set; }
        public double Spendtime { get; set; }
        public int Spendmem1 { get; set; }
        public int Spendmem2 { get; set; }
        public string Info { get; set; }
        public string OnlyCompile { get; set; }
        public byte JudgeServer { get; set; }
        public string IpAddr { get; set; }
        public string Balloon { get; set; }
    }
}
