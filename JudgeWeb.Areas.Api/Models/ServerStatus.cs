﻿namespace JudgeWeb.Areas.Api.Models
{
    public class ServerStatus
    {
        public int cid { get; set; }
        public int num_submissions { get; set; }
        public int num_queued { get; set; }
        public int num_judging { get; set; }
    }
}
