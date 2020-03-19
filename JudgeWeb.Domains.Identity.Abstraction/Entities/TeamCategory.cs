namespace JudgeWeb.Data
{
    /// <summary>
    /// 队伍目录
    /// </summary>
    public class TeamCategory
    {
        /// <summary>
        /// 分组编号
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// 分组名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 分组颜色
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 是否展示给公众
        /// </summary>
        public bool IsPublic { get; set; }
    }
}
