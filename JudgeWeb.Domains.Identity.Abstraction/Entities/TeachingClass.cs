using System.Collections.Generic;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 教学班信息
    /// </summary>
    public class TeachingClass
    {
        /// <summary>
        /// 教学班编号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 教学班名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 学生集合
        /// </summary>
        public ICollection<ClassStudent> Collection { get; set; }

        /// <summary>
        /// 教学班人数
        /// </summary>
        public int Count { get; set; }
    }
}
