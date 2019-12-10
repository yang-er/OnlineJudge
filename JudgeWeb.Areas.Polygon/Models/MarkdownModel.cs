namespace JudgeWeb.Areas.Polygon.Models
{
    public class MarkdownModel
    {
        /// <summary>
        /// Markdown实际内容
        /// </summary>
        public string Markdown { get; set; }
        
        /// <summary>
        /// 后部储存代码
        /// </summary>
        public string BackingStore { get; set; }

        /// <summary>
        /// 目标内容
        /// </summary>
        public string Target { get; set; }
    }
}
