namespace JudgeWeb.Areas.Contest.Models
{
    public class ScoreboardCellModel
    {
        public int? SolveTime { get; set; }

        public int PendingCount { get; set; }

        public int JudgedCount { get; set; }

        public bool IsFirstToSolve { get; set; }

        public string StyleClass
        {
            get
            {
                if (PendingCount > 0)
                    return "score_pending";
                else if (SolveTime.HasValue && IsFirstToSolve)
                    return "score_correct score_first";
                else if (SolveTime.HasValue)
                    return "score_correct";
                else
                    return "score_incorrect";
            }
        }

        public string Tries
        {
            get
            {
                string ans = "";
                if (PendingCount > 0)
                    ans += JudgedCount + " + " + PendingCount;
                else if (JudgedCount > 0)
                    ans += JudgedCount;
                if (PendingCount + JudgedCount > 1)
                    ans += " tries";
                else
                    ans += " try";
                return ans;
            }
        }
    }
}
