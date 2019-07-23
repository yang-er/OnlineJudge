using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Api.Models
{
    public class CreateJudgeHostModel
    {
        [Required]
        public string hostname { get; set; }
    }
}
