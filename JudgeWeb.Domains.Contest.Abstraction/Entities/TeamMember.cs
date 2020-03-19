namespace JudgeWeb.Data
{
    public class TeamMember
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 队伍编号
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 是否为临时账号
        /// </summary>
        public bool Temporary { get; set; }
    }
}
