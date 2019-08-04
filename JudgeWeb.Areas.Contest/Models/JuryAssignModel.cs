using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryAssignModel
    {
        [DisplayName("User Name")]
        [Required]
        public string UserName { get; set; }
    }
}
