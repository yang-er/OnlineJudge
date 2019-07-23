using JudgeWeb.Data;

namespace JudgeWeb.Areas.Judge.Models
{
    public class StatusListModel
    {
        public Judging Grade { get; set; }
        public Submission Submission { get; set; }
        public string UserName { get; set; }
        public string Language { get; set; }
    }
}
