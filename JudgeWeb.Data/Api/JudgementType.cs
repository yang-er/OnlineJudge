namespace JudgeWeb.Data.Api
{
    [EntityType("judgement-types")]
    public class JudgementType : ContestEventEntity
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
            switch (verdict)
            {
                case "CE":
                    return Verdict.CompileError;
                case "MLE":
                    return Verdict.MemoryLimitExceeded;
                case "OLE":
                    return Verdict.OutputLimitExceeded;
                case "RTE":
                    return Verdict.RuntimeError;
                case "TLE":
                    return Verdict.TimeLimitExceeded;
                case "WA":
                    return Verdict.WrongAnswer;
                case "PE":
                    return Verdict.PresentationError;
                case "AC":
                    return Verdict.Accepted;
                case "JE":
                    return Verdict.UndefinedError;
                default:
                    return Verdict.Unknown;
            }
        }

        public static string For(Verdict verdict)
        {
            switch (verdict)
            {
                case Verdict.TimeLimitExceeded:
                    return Defaults[4].id;
                case Verdict.MemoryLimitExceeded:
                    return Defaults[1].id;
                case Verdict.RuntimeError:
                    return Defaults[3].id;
                case Verdict.OutputLimitExceeded:
                    return Defaults[2].id;
                case Verdict.WrongAnswer:
                    return Defaults[5].id;
                case Verdict.CompileError:
                    return Defaults[0].id;
                case Verdict.PresentationError:
                    return Defaults[6].id;
                case Verdict.Accepted:
                    return Defaults[7].id;
                case Verdict.Pending:
                case Verdict.Running:
                    return null;
                case Verdict.Unknown:
                case Verdict.UndefinedError:
                default:
                    return Defaults[8].id;
            }
        }
    }
}
