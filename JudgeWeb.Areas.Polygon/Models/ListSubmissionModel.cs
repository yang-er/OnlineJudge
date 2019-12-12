using JudgeWeb.Data;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Polygon.Models
{
    public class ListSubmissionModel
    {
        public int SubmissionId { get; set; }

        public int JudgingId { get; set; }

        public DateTimeOffset Time { get; set; }

        //public bool Verified { get; set; }

        //public string VerifiedBy { get; set; }

        public string UserName { get; set; }

        public string Language { get; set; }

        public int ExecutionTime { get; set; }

        public Verdict? Expected { get; set; }

        public Verdict Result { get; set; }

        public IDictionary<int, JudgingDetailModel> Details { get; set; }
    }
}
