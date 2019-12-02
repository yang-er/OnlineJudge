namespace JudgeWeb.Areas.Api.Models
{
    public class SubmissionFile
    {
        public string id { get; set; }
        public string submission_id { get; set; }
        public string filename { get; set; }
        public string source { get; set; }
    }
}
