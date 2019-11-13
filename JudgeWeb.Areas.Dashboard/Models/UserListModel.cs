using JudgeWeb.Data;
using System.Collections.Generic;

namespace JudgeWeb.Areas.Dashboard.Models
{
    public class UserListModel
    {
        public IEnumerable<(User, IEnumerable<string>)> List { get; set; }

        public Dictionary<int, Role> Roles { get; set; }

        public int TotalPage { get; set; }

        public int CurrentPage { get; set; }
    }
}
