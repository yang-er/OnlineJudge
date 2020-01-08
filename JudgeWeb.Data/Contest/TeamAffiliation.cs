﻿namespace JudgeWeb.Data
{
    /// <summary>
    /// 队伍归属组织
    /// </summary>
    public class TeamAffiliation
    {
        /// <summary>
        /// 组织编号
        /// </summary>
        [Key]
        public int AffiliationId { get; set; }

        /// <summary>
        /// 组织正式名称
        /// </summary>
        public string FormalName { get; set; }

        /// <summary>
        /// 外部名称
        /// </summary>
        [Index]
        public string ExternalId { get; set; }

        /// <summary>
        /// 国家编号
        /// </summary>
        [NonUnicode(MaxLength = 4)]
        public string CountryCode { get; set; } = "CHN";
    }
}
