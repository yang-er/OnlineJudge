namespace JudgeWeb.Data
{
    /// <summary>
    /// 班级学生关系组
    /// </summary>
    public class ClassStudent
    {
        /// <summary>
        /// 学生编号
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// 行政班编号
        /// </summary>
        public int ClassId { get; set; }
    }
}
