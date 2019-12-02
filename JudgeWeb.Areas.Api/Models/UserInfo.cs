using System.Collections.Generic;

namespace JudgeWeb.Areas.Api.Models
{
    public class UserInfo
    {
        public IList<string> roles { get; set; }
        public int id { get; set; }
        public string username { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string lastip { get; set; }
    }
}
