namespace JudgeWeb.Areas.Api.Models
{
    public class ContestLanguage
    {
        public string id { get; set; }
        public string name { get; set; }
        public string[] extensions { get; set; }
        public bool allow_judge { get; set; }
        public double time_factor { get; set; }
        public bool require_entry_point { get; set; }
        public string entry_point_description { get; set; }
    }
}
