using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Polygon.Models
{
    public class ProblemEditModel
    {
        [DisplayName("Problem ID")]
        public int ProblemId { get; set; }

        [DisplayName("Name")]
        [Required]
        public string Title { get; set; }

        [DisplayName("Timelimit")]
        [Range(500, 15000)]
        public int TimeLimit { get; set; }

        [DisplayName("Memlimit")]
        [Range(32768, 1048576)]
        public int MemoryLimit { get; set; }

        [DisplayName("Outputlimit")]
        [Range(4096, 40960)]
        public int OutputLimit { get; set; }

        [DisplayName("Source")]
        public string Source { get; set; }

        [DisplayName("Run script")]
        public string RunScript { get; set; }

        [DisplayName("Compare script")]
        public string CompareScript { get; set; }

        [DisplayName("Compare script arguments")]
        public string CompareArgument { get; set; }

        [DisplayName("Use run script as compare script")]
        public bool RunAsCompare { get; set; }

        [DisplayName("Allow users to download test-data in Gym")]
        public bool Shared { get; set; }

        public IFormFile UploadedCompare { get; set; }

        public IFormFile UploadedRun { get; set; }
    }
}
