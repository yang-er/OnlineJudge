using Microsoft.AspNetCore.Identity;

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
    }
}
