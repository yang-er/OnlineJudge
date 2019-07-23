using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Account.Models
{
    public class IndexViewModel
    {
        public string Username { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Nick name")]
        public string NickName { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        public string StatusMessage { get; set; }
    }
}
