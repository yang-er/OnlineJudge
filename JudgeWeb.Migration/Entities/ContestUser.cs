using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class ContestUser
    {
        public int Cid { get; set; }
        public int Uid { get; set; }
        public string Status { get; set; }
        public byte Usertype { get; set; }
        public string TeamName { get; set; }
        public string PersonalName { get; set; }
        public string Sex { get; set; }
        public string Email { get; set; }
        public string Telphone { get; set; }
        public string SchoolId { get; set; }
        public string University { get; set; }
        public string Department { get; set; }
        public string Grade { get; set; }
        public short? Pc2name { get; set; }
        public string Pc2pwd { get; set; }
        public string Ip { get; set; }
        public string HidePrivacy { get; set; }
        public short? Rank { get; set; }
        public byte? SolveCnt { get; set; }
    }
}
