using JudgeWeb.Data;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Account.Models
{
    public class StudentVerifyModel
    {
        [Display(Name = "Student ID")]
        public int StudentId { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Student Name")]
        public string StudentName { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [EmailAddress]
        [Display(Name = "Student Email")]
        public string Email { get; set; }
    }
}
