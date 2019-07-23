using Microsoft.AspNetCore.Http;
using System.ComponentModel;

namespace JudgeWeb.Areas.Judge.Models
{
    public class ExecutableUploadModel
    {
        [DisplayName("ID")]
        public string ID { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        [DisplayName("Type")]
        public string Type { get; set; }

        [DisplayName("Upload Content")]
        public IFormFile UploadContent { get; set; }
    }
}
