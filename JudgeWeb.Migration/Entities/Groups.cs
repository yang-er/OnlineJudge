using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class Groups
    {
        public int Gid { get; set; }
        public string Name { get; set; }
        public string Plan { get; set; }
        public string ImageFile { get; set; }
        public DateTime DateCreate { get; set; }
        public int? Creator { get; set; }
        public string Open { get; set; }
    }
}
