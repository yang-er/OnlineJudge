namespace JudgeWeb.Areas.Judge.Models
{
    public class CodeRestoreModel
    {
        public long Time { get; set; }
        public int Author { get; set; }
        public int ProblemId { get; set; }
        public int Status { get; set; }
        public int ExecuteTime { get; set; }
        public int ExecuteMemory { get; set; }
        public int Language { get; set; }
        public string Ip { get; set; }
        public int Server { get; set; }
        public int Grade { get; set; }
        public string SourceCode { get; set; }
        public string CompileError { get; set; }
    }
}
