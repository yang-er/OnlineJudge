using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data.Ext
{
    /// <summary>
    /// 气球
    /// </summary>
    public class Balloon
    {
        /// <summary>
        /// 气球编号
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 提交编号
        /// </summary>
        [HasOneWithMany(typeof(Submission), DeleteBehavior.Cascade)]
        public int SubmissionId { get; set; }

        /// <summary>
        /// 是否已经分发出去
        /// </summary>
        public bool Done { get; set; }
    }
}
