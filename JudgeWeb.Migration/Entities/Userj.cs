using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Userj
    {
        public int Uid { get; set; }
        public string Userid { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public DateTime RegDate { get; set; }
        public string Mail { get; set; }
        public string University { get; set; }
        public string Dept { get; set; }
        public string Plan { get; set; }
        public int Family { get; set; }
        public string Sender { get; set; }
        public string Status { get; set; }
        public string Usertype { get; set; }
        public byte Medal { get; set; }
        public string PwdSave { get; set; }
    }
}
