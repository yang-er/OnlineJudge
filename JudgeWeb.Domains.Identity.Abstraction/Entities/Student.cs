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
        public int Id { get; set; }

        /// <summary>
        /// 学生名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 对应本站用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 学生邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool? IsVerified { get; set; }
    }
}
