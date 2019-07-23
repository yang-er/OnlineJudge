namespace JudgeWeb.Data
{
    /// <summary>
    /// 与DOMjudge兼容的设置
    /// </summary>
    public class Configure
    {
        /// <summary>
        /// 设置编号
        /// </summary>
        [Key]
        public int ConfigId { get; set; }

        /// <summary>
        /// 设置名称
        /// </summary>
        [Index]
        [Property(IsRequired = true, MaxLength = 128, IsUnicode = false)]
        public string Name { get; set; }

        /// <summary>
        /// 设置值
        /// </summary>
        [Property(IsRequired = true, IsUnicode = false)]
        public string Value { get; set; }

        /// <summary>
        /// 设置类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 是否显示在网页上
        /// </summary>
        public int Public { get; set; }

        /// <summary>
        /// 设置分类
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 设置描述
        /// </summary>
        public string Description { get; set; }
    }
}
