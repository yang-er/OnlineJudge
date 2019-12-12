using System.ComponentModel;

namespace JudgeWeb.Areas.Polygon.Models
{
    public class CodeSubmitModel
    {
        [DisplayName("Language")]
        public int Language { get; set; }

        [DisplayName("Source Code")]
        public string Code { get; set; }
    }
}
