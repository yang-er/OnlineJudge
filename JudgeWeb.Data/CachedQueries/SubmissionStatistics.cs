using EntityFrameworkCore.Cacheable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JudgeWeb.Data
{
    public class SubmissionStatistics
    {
        public int TotalSubmission { get; set; }

        public int AcceptedSubmission { get; set; }

        public int ProblemId { get; set; }

        public int Author { get; set; }

        public int ContestId { get; set; }
    }

    public static partial class QueryExtensions
    {
        public static IEnumerable<SubmissionStatistics> SubmissionStatistics(this AppDbContext dbContext)
        {
            return dbContext.SubmissionStatistics
                .Cacheable(TimeSpan.FromMinutes(10))
                .ToList();
        }
    }
}
