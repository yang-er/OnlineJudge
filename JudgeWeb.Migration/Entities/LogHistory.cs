using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class LogHistory
    {
        public int Id { get; set; }
        public string Userid { get; set; }
        public DateTime LogTime { get; set; }
        public string IpAddr { get; set; }
        public int? Port { get; set; }
        public string Status { get; set; }
        public string WrongPwd { get; set; }
    }
}
