using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Contest.Models
{
    public class AddPrintModel
    {
        [Required]
        [DisplayName("Source File")]
        public IFormFile SourceFile { get; set; }

        [Required]
        [DisplayName("Language")]
        public string Language { get; set; }
    }
}
