using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Newso
    {
        public int NewsId { get; set; }
        public string Message { get; set; }
        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Seq { get; set; }
    }
}
