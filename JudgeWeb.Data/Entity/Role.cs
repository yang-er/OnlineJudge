using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 权限
    /// </summary>
    public class Role : IdentityRole<int>
    {
        /// <summary>
        /// 构造一个权限。
        /// </summary>
        public Role() { }

        /// <summary>
        /// 构造一个权限。
        /// </summary>
        /// <param name="roleName">权限名</param>
        public Role(string roleName) : this()
        {
            Name = roleName;
        }

        /// <summary>
        /// 简写名称
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// 描述内容
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        [HasOneWithMany(typeof(Problem), DeleteBehavior.Cascade)]
        public int? ProblemId { get; set; }

        /// <summary>
        /// 实体类型，1为题目，2为比赛
        /// </summary>
        [HasOneWithMany(typeof(Contest), DeleteBehavior.Cascade)]
        public int? ContestId { get; set; }
    }
}
