namespace JudgeWeb.Data
{
    /// <summary>
    /// 学生信息
    /// </summary>
    public class Student
    {
        /// <summary>
        /// 学生教学号
        /// </summary>
        [Key(ValueGeneratedNever = true)]
        public int Id { get; set; }

        /// <summary>
        /// 学生名称
        /// </summary>
        [MaxLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// 学生行政班
        /// </summary>
        public int Class { get; set; }
    }
}
