using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Contest.Models
{
    public class AddClarificationModel
    {
        public int? ReplyTo { get; set; }

        [DisplayName("Send to")]
        public int TeamTo { get; set; }

        [Required]
        [DisplayName("Message")]
        public string Body { get; set; }

        [DisplayName("Subject")]
        public string Type { get; set; }
    }
}
