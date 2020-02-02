using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Dashboard.Models
{
    public class AddStudentsBatchModel
    {
        [Required]
        public string Students { get; set; }
    }
}
