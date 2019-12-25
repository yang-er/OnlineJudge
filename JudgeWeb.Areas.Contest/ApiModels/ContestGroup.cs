namespace JudgeWeb.Areas.Api.Models
{
    public class ContestGroup
    {
        public bool hidden { get; set; }
        public string icpc_id { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public int sortorder { get; set; }
        public string color { get; set; }
    }
}
