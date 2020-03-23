using JudgeWeb.Data;

namespace JudgeWeb.Areas.Api.Models
{
    public class TestcaseToJudge
    {
        public int testcaseid { get; set; }
        public string md5sum_input { get; set; }
        public string md5sum_output { get; set; }
        public int probid { get; set; }
        public int rank { get; set; }
        public string description_as_string { get; set; }

        public TestcaseToJudge(Testcase t)
        {
            testcaseid = t.TestcaseId;
            probid = t.ProblemId;
            md5sum_input = t.Md5sumInput;
            md5sum_output = t.Md5sumOutput;
            rank = t.Rank;
            probid = t.ProblemId;
            description_as_string = t.Description;
        }
    }
}
