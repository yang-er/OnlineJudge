using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JudgeWeb.Areas.Contest.Models
{
    public class JuryEditModel
    {
        [ReadOnly(true)]
        public int ContestId { get; set; }

        [Required]
        [DisplayName("Ranking strategy")]
        public int RankingStrategy { get; set; }

        [Required]
        [DisplayName("Golden medal rank")]
        public int GoldenMedal { get; set; }

        [Required]
        [DisplayName("Silver medal rank")]
        public int SilverMedal { get; set; }

        [Required]
        [DisplayName("Bronze medal rank")]
        public int BronzeMedal { get; set; }

        [Required]
        [DisplayName("Is active and visible to public")]
        public bool IsPublic { get; set; }

        [Required]
        [DisplayName("Create balloons")]
        public bool UseBalloon { get; set; }

        [Required]
        [DisplayName("Send printings")]
        public bool UsePrintings { get; set; }

        [Required]
        [DisplayName("Self-registered category")]
        public int DefaultCategory { get; set; }

        [Required]
        [DisplayName("Shortname")]
        public string ShortName { get; set; }

        [Required]
        [DisplayName("Name")]
        public string Name { get; set; }

        [DateTime]
        [DisplayName("Start time")]
        public string StartTime { get; set; }

        [TimeSpan]
        [DisplayName("Scoreboard freeze time")]
        public string FreezeTime { get; set; }

        [Required]
        [TimeSpan]
        [DisplayName("End time")]
        public string StopTime { get; set; }

        [TimeSpan]
        [DisplayName("Scoreboard unfreeze time")]
        public string UnfreezeTime { get; set; }
    }
}
