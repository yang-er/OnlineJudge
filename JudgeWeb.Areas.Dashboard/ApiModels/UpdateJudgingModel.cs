namespace JudgeWeb.Areas.Api.Models
{
    public class UpdateJudgingModel
    {
        /// <summary>
        /// Whether compile has succeeded
        /// </summary>
        public int compile_success { get; set; }

        /// <summary>
        /// Output contents from compiler
        /// </summary>
        public string output_compile { get; set; }
    }
}
