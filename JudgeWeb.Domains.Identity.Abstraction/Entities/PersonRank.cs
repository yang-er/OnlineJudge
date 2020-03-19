namespace JudgeWeb.Data
{
    /// <summary>
    /// 个人做题排名
    /// </summary>
    public class PersonRank
    {
        /// <summary>
        /// 内部的条目ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 年级
        /// </summary>
        public int Grade { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string ACMer { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// OJ分类
        /// </summary>
        public int Category { get; set; }

        /// <summary>
        /// 积分
        /// </summary>
        public int Result { get; set; }
    }
}
