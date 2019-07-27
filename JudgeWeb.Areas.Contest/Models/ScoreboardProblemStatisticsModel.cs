namespace JudgeWeb.Areas.Contest.Models
{
    public class ScoreboardProblemStatisticsModel
    {
        public int Accepted { get; set; }

        public int Rejected { get; set; }

        public int Pending { get; set; }

        public int? FirstSolve { get; set; }
    }
}
