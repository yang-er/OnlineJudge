using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Usero
    {
        public string Userid { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
        public string Unianddep { get; set; }
        public string Plan { get; set; }
        public string Sender { get; set; }
        public string Status { get; set; }
        public string Usertype { get; set; }
        public string Lastip { get; set; }
        public int Point { get; set; }
    }
}
