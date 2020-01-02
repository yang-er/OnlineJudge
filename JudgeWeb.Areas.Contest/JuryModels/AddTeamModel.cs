using System.ComponentModel;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryAddTeamModel : JuryEditTeamModel
    {
        [DisplayName("User name")]
        public string UserName { get; set; }
    }
}
