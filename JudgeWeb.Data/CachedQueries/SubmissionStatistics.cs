namespace JudgeWeb.Data
{
    public class SubmissionStatistics
    {
        public int TotalSubmission { get; set; }

        public int AcceptedSubmission { get; set; }

        public int ProblemId { get; set; }

        public int Author { get; set; }

        public int ContestId { get; set; }

        internal const string QueryString =
            "SELECT COUNT(*) AS TotalSubmission," +
                  " SUM(CASE WHEN g.Status = 11 THEN 1 ELSE 0 END) AS AcceptedSubmission," +
                  " s.ProblemId, s.ContestId, s.Author " +
            "FROM Submissions AS s " +
            "JOIN Judgings AS g ON g.SubmissionId = s.SubmissionId AND g.Active = 1 " +
            "GROUP BY s.ProblemId, s.Author, s.ContestId";
    }
}
