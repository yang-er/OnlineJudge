using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Problemo
    {
        public int Pid { get; set; }
        public int Cid { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public short Timelimit { get; set; }
        public int Memorylimit { get; set; }
        public string Intro { get; set; }
        public DateTime? Date { get; set; }
        public string Visible { get; set; }
        public short Diff { get; set; }
        public string SpecJudge { get; set; }
        public byte Challenge { get; set; }
        public int Point { get; set; }
        public string Tags { get; set; }
    }
}
