using Microsoft.AspNetCore.Http;
using System.ComponentModel;

namespace JudgeWeb.Areas.Polygon.Models
{
    public class TestcaseUploadModel
    {
        [DisplayName("Secret data, not shown to public")]
        public bool IsSecret { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        [DisplayName("Point")]
        public int Point { get; set; }

        [DisplayName("Input Content")]
        public IFormFile InputContent { get; set; }

        [DisplayName("Output Content")]
        public IFormFile OutputContent { get; set; }
    }
}
