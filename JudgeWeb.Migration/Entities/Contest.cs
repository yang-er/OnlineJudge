using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Contesto
    {
        public int Cid { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public short Bonus { get; set; }
        public short Prize { get; set; }
        public short Fee { get; set; }
        public string OpenType { get; set; }
        public string JudgeType { get; set; }
        public byte RunStatus { get; set; }
        public string Descr { get; set; }
        public string Visible { get; set; }
        public string Pwd { get; set; }
        public int LastBalloonSid { get; set; }
    }
}
