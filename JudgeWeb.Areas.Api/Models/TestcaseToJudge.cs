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
    }
}
