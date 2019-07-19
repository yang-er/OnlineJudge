using System;

namespace JudgeWeb.Features.OjUpdate
{
    public class OjAccount : IComparable<OjAccount>
    {
        public int Grade { get; set; }
        public string Account { get; set; }
        public string NickName { get; set; }
        public int Solved { get; set; } = -1;

        public int CompareTo(OjAccount other)
        {
            if (Solved == other.Solved)
                return -Grade.CompareTo(other.Grade);
            return -Solved.CompareTo(other.Solved);
        }
    }
}
