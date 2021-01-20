using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class SubmitHide
    {
        public int Sid { get; set; }
        public int Uid { get; set; }
        public string Userid { get; set; }
        public int Pid { get; set; }
        public DateTime Sdate { get; set; }
        public string Lang { get; set; }
        public byte Status { get; set; }
        public int Slen { get; set; }
        public double Spendtime { get; set; }
        public int Spendmem1 { get; set; }
        public int Spendmem2 { get; set; }
        public string Isopen { get; set; }
        public short Cid { get; set; }
        public byte PSolve { get; set; }
        public byte PCont { get; set; }
        public byte PTop { get; set; }
        public byte PBest { get; set; }
        public byte JudgeServer { get; set; }
    }
}
