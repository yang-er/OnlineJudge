using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class UserProbStat
    {
        public int Uid { get; set; }
        public int Pid { get; set; }
        public int ContSeq { get; set; }
        public int FirstSid { get; set; }
        public int SolveSid { get; set; }
        public int BestSid { get; set; }
        public byte? Score { get; set; }
        public DateTime? DateScore { get; set; }
    }
}
