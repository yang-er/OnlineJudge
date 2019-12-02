namespace JudgeWeb.Areas.Dashboard.Models
{
    public class NewsEditModel
    {
        public int NewsId { get; set; }

        public string Title { get; set; }

        public bool Active { get; set; }

        public string MarkdownSource { get; set; }
    }
}
