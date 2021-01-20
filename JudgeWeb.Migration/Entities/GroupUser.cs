using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class GroupUser
    {
        public int Gid { get; set; }
        public int Uid { get; set; }
        public DateTime JoinDate { get; set; }
        public byte Slevel { get; set; }
        public string TrueName { get; set; }
    }
}
