using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class ProbStat
    {
        public int Pid { get; set; }
        public int CAll { get; set; }
        public int CPass { get; set; }
        public int CAc { get; set; }
        public int CWa { get; set; }
        public int CPe { get; set; }
        public int CRe { get; set; }
        public int CTle { get; set; }
        public int CMle { get; set; }
        public int CCe { get; set; }
        public int COther { get; set; }
        public int BestSid { get; set; }
    }
}
