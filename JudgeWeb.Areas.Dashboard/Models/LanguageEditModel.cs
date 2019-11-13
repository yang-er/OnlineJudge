using System.ComponentModel;

namespace JudgeWeb.Areas.Dashboard.Models
{
    public class LanguageEditModel
    {
        [DisplayName("External ID")]
        public string ExternalId { get; set; }

        [DisplayName("Name")]
        public string Name { get; set; }

        [DisplayName("Time factor")]
        public double TimeFactor { get; set; }

        [DisplayName("Compile script")]
        public string CompileScript { get; set; }

        [DisplayName("File extension")]
        public string FileExtension { get; set; }
    }
}
