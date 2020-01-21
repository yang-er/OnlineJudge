namespace JudgeWeb.Data.Api
{
    [EntityType("languages")]
    public class ContestLanguage : ContestEventEntity
    {
        public string name { get; set; }
        public string[] extensions { get; set; }
        public bool allow_judge { get; set; }
        public double time_factor { get; set; }
        public bool require_entry_point { get; set; }
        public string entry_point_description { get; set; }

        public ContestLanguage() { }

        public ContestLanguage(Language l)
        {
            allow_judge = l.AllowJudge;
            time_factor = l.TimeFactor;
            extensions = new[] { l.FileExtension };
            id = l.Id;
            name = l.Name;
        }
    }
}
