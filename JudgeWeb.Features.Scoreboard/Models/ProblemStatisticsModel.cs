namespace JudgeWeb.Features.Scoreboard
{
    public class ProblemStatisticsModel
    {
        public int Accepted { get; set; }

        public int Rejected { get; set; }

        public int Pending { get; set; }

        public int? FirstSolve { get; set; }
    }
}
