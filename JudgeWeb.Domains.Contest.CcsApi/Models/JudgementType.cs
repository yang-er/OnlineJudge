namespace JudgeWeb.Domains.Contests.ApiModels
{
    [EntityType("judgement-types")]
    public class JudgementType : EventEntity
    {
        public JudgementType(string i, string n, bool p, bool s)
        {
            id = i;
            name = n;
            penalty = p;
            solved = s;
        }

        public string name { get; set; }
        public bool penalty { get; set; }
        public bool solved { get; set; }

        public static readonly JudgementType[] Defaults = new[]
        {
            new JudgementType("CE", "compiler error", false, false),
            new JudgementType("MLE", "memory limit", true, false),
            new JudgementType("OLE", "output limit", true, false),
            new JudgementType("RTE", "run error", true, false),
            new JudgementType("TLE", "timelimit", true, false),
            new JudgementType("WA", "wrong answer", true, false),
            new JudgementType("PE", "presentation error", true, false),
            new JudgementType("AC", "correct", false, true),
            new JudgementType("JE", "judge error", false, false),
        };

        public static Verdict For(string verdict)
        {
            return verdict switch
            {
                "CE" => Verdict.CompileError,
                "MLE" => Verdict.MemoryLimitExceeded,
                "OLE" => Verdict.OutputLimitExceeded,
                "RTE" => Verdict.RuntimeError,
                "TLE" => Verdict.TimeLimitExceeded,
                "WA" => Verdict.WrongAnswer,
                "PE" => Verdict.PresentationError,
                "AC" => Verdict.Accepted,
                "JE" => Verdict.UndefinedError,
                _ => Verdict.Unknown,
            };
        }

        public static string For(Verdict verdict)
        {
            return verdict switch
            {
                Verdict.TimeLimitExceeded => Defaults[4].id,
                Verdict.MemoryLimitExceeded => Defaults[1].id,
                Verdict.RuntimeError => Defaults[3].id,
                Verdict.OutputLimitExceeded => Defaults[2].id,
                Verdict.WrongAnswer => Defaults[5].id,
                Verdict.CompileError => Defaults[0].id,
                Verdict.PresentationError => Defaults[6].id,
                Verdict.Accepted => Defaults[7].id,
                Verdict.Pending => null,
                Verdict.Running => null,
                Verdict.Unknown => Defaults[8].id,
                Verdict.UndefinedError => Defaults[8].id,
                _ => Defaults[8].id,
            };
        }
    }
}
