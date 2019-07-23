namespace JudgeWeb.Areas.Api.Models
{
    public class UpdateJudgingModel
    {
        public int compile_success { get; set; }

        public string output_compile { get; set; }

        public string entry_point { get; set; }
    }
}
