using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class TeamPwd
    {
        public int Id { get; set; }
        public string Userid { get; set; }
        public byte Round { get; set; }
        public string Pwd { get; set; }
    }
}
