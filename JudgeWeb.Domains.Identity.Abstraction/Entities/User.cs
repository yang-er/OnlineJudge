﻿using Microsoft.AspNetCore.Identity;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 用户
    /// </summary>
    public class User : IdentityUser<int>
    {
        /// <summary>
        /// 构造一个用户。
        /// </summary>
        public User() { }

        /// <summary>
        /// 构造一个带用户名的用户。
        /// </summary>
        /// <param name="userName">用户名</param>
        public User(string userName) : this()
        {
            UserName = userName;
        }

        /// <summary>
        /// 用户昵称
        /// </summary>
        [PersonalData]
        public string NickName { get; set; }

        /// <summary>
        /// 签名档
        /// </summary>
        [PersonalData]
        public string Plan { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTimeOffset? RegisterTime { get; set; }

        /// <summary>
        /// 学生教学号
        /// </summary>
        public int? StudentId { get; set; }

        /// <summary>
        /// 学生邮箱
        /// </summary>
        public string StudentEmail { get; set; }

        /// <summary>
        /// 学生已验证
        /// </summary>
        public bool StudentVerified { get; set; }
    }
}
