using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Contest.Models
{
    public class RestoreSubmissionModel
    {
        [Required]
        public int probid { get; set; }

        [Required]
        public int teamid { get; set; }

        [Required]
        public string langid { get; set; }

        [Required]
        public string code { get; set; }

        [Required]
        public string ip { get; set; }
    }
}
