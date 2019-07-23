namespace JudgeWeb.Areas.Judge.Models
{
    public class DetailRestoreModel
    {
        public int RunningId { get; set; }
        public int Status { get; set; }
        public int ExecuteMemory { get; set; }
        public int ExecuteTime { get; set; }
        public int ExitCode { get; set; }
    }
}
