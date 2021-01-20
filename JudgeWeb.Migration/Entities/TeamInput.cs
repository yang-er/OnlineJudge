using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class TeamInput
    {
        public int Id { get; set; }
        public int Cid { get; set; }
        public string Seq { get; set; }
        public string Userid { get; set; }
        public string Pwd { get; set; }
        public string Uni { get; set; }
        public string TeamName { get; set; }
        public string Email { get; set; }
        public string Tel { get; set; }
        public string Tag { get; set; }
        public byte Usertype { get; set; }
    }
}
