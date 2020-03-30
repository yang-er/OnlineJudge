using System;
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
        [DisplayName("Status availability")]
        public int StatusAvailable { get; set; }

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

        public JuryEditModel() { }

        public JuryEditModel(Data.Contest cont)
        {
            var startTime = cont.StartTime?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "";
            var startDateTime = cont.StartTime ?? DateTimeOffset.UnixEpoch;
            var stopTime = (cont.EndTime - startDateTime)?.ToDeltaString() ?? "";
            var unfTime = (cont.UnfreezeTime - startDateTime)?.ToDeltaString() ?? "";
            var freTime = (cont.FreezeTime - startDateTime)?.ToDeltaString() ?? "";

            ContestId = cont.ContestId;
            FreezeTime = freTime;
            Name = cont.Name;
            ShortName = cont.ShortName;
            RankingStrategy = cont.RankingStrategy;
            StartTime = startTime;
            StopTime = stopTime;
            UnfreezeTime = unfTime;
            DefaultCategory = cont.RegisterDefaultCategory;
            IsPublic = cont.IsPublic;
            UsePrintings = cont.PrintingAvaliable;
            UseBalloon = cont.BalloonAvaliable;
            StatusAvailable = cont.StatusAvaliable;
        }
    }
}
