using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Account.Models
{
    public class ExternalLoginModel
    {
        [Required]
        [UserName]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
