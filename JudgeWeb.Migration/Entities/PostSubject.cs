using System;
using System.Collections.Generic;

namespace JudgeWeb.Migration
{
    public partial class PostSubject
    {
        public string Category { get; set; }
        public int Pid { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime PostDate { get; set; }
        public string Status { get; set; }
        public int SubNum { get; set; }
        public int LastPostId { get; set; }
    }
}
