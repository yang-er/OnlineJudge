using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JudgeWeb.Areas.Api.Models
{
    public class JudgingRunModel
    {
        static Dictionary<string, Verdict> Mapping { get; }

        public string testcaseid { get; set; }
        public string runresult { get; set; }
        public string runtime { get; set; }
        public string output_run { get; set; }
        public string output_system { get; set; }
        public string output_diff { get; set; }
        public string output_error { get; set; }

        static JudgingRunModel()
        {
            Mapping = new Dictionary<string, Verdict>
            {
                { "correct", Verdict.Accepted },
                { "no-output", Verdict.WrongAnswer },
                { "wrong-answer", Verdict.WrongAnswer },
                { "timelimit", Verdict.TimeLimitExceeded },
                { "memory-limit", Verdict.MemoryLimitExceeded },
                { "output-limit", Verdict.OutputLimitExceeded },
                { "compare-error", Verdict.UndefinedError },
                { "run-error", Verdict.RuntimeError },
            };
        }

        public Detail ParseInfo(int jid, DateTimeOffset time2)
        {
            int time, mem, exitcode, tcid;
            if (!double.TryParse(runtime, out var dtime)) dtime = 0;
            time = (int)(dtime * 1000);
            if (!Mapping.TryGetValue(runresult, out var verdict))
                verdict = Verdict.UndefinedError;
            if (!int.TryParse(testcaseid, out tcid)) tcid = 0;

            try
            {
                var outsys = Encoding.UTF8.GetString(Convert.FromBase64String(output_system));
                var st = Regex.Match(outsys, @"memory used: (\S+) bytes");
                if (!(st.Success && int.TryParse(st.Groups[1].Value, out mem))) mem = 0;
                mem /= 1024;
                var st2 = Regex.Match(outsys, @"Non-zero exitcode (\S+)");
                if (!(st2.Success && int.TryParse(st2.Groups[1].Value, out exitcode))) exitcode = 0;
            }
            catch
            {
                mem = exitcode = 0;
            }

            return new Detail
            {
                Status = verdict,
                ExecuteMemory = mem,
                ExecuteTime = time,
                TestcaseId = tcid,
                OutputDiff = output_diff,
                OutputSystem = output_system,
                JudgingId = jid,
                CompleteTime = time2,
            };
        }
    }
}
