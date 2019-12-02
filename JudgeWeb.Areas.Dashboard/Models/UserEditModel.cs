namespace JudgeWeb.Areas.Dashboard.Models
{
    public class UserEditModel
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public string NickName { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public int[] Roles { get; set; }
    }
}
