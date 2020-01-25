using System;

namespace JudgeWeb.Features.Scoreboard
{
    public class ScoreboardEventArgs : EventArgs
    {
        public int EventType { get; set; }
        public int ProblemId { get; set; }
        public int ContestId { get; set; }
        public int TeamId { get; set; }
        public int SubmissionId { get; set; }
        public int TotalScore { get; set; }
        public int RankStrategy { get; set; }
        public Verdict? Verdict { get; set; }
        public DateTimeOffset SubmitTime { get; set; }
        public DateTimeOffset ContestTime { get; set; }
        public DateTimeOffset? FreezeTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public bool Frozen { get; set; }
        public bool Balloon { get; set; }
    }
}
