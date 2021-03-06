﻿using Microsoft.AspNetCore.Identity;

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
        public Role(string roleName)
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
        public int? ProblemId { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int? ContestId { get; set; }
    }
}
