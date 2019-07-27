using JudgeWeb.Data;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryHomeModel
    {
        public Data.Contest Contest { get; set; }

        public ContestProblem[] Problem { get; set; }

        public string Message { get; set; }
    }
}
