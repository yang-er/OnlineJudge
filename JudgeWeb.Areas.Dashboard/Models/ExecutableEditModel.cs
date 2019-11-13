using Microsoft.AspNetCore.Http;

namespace JudgeWeb.Areas.Dashboard.Models
{
    public class ExecutableEditModel
    {
        public string ExecId { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }

        public IFormFile Archive { get; set; }
    }
}
