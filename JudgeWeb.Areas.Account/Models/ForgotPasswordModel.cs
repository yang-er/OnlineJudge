using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Account.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
