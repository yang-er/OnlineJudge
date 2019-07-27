using System.ComponentModel;

namespace JudgeWeb.Areas.Contest.Models
{
    public class TeamCodeSubmitModel
    {
        [DisplayName("Problem")]
        public string Problem { get; set; }

        [DisplayName("Language")]
        public int Language { get; set; }

        [DisplayName("Source Code")]
        public string Code { get; set; }
    }
}
