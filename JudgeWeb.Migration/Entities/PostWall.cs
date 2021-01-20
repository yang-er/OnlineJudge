using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class PostWall
    {
        public int PostId { get; set; }
        public string Category { get; set; }
        public int Pid { get; set; }
        public int Uid { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime PostDate { get; set; }
        public byte Priority { get; set; }
        public string Status { get; set; }
        public int ReplyId { get; set; }
    }
}
