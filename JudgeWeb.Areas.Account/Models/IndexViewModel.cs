﻿using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Account.Models
{
    public class IndexViewModel
    {
        public string Username { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Nick name")]
        public string NickName { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Plan")]
        public string Plan { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
