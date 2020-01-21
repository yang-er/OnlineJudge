using System;

namespace JudgeWeb.Areas.Contest.Models
{
    public class ShowPrintModel
    {
        public int Id { get; set; }

        public DateTimeOffset Time { get; set; }

        public string TeamName { get; set; }

        public string Location { get; set; }

        public string FileName { get; set; }

        public string Language { get; set; }

        public bool? Done { get; set; }
    }
}
