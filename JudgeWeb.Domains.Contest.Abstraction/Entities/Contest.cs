using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 比赛状态
    /// </summary>
    public enum ContestState
    {
        /// <summary>
        /// 未计划实践
        /// </summary>
        NotScheduled,

        /// <summary>
        /// 未开始
        /// </summary>
        ScheduledToStart,

        /// <summary>
        /// 已开始
        /// </summary>
        Started,

        /// <summary>
        /// 已封榜
        /// </summary>
        Frozen,

        /// <summary>
        /// 已结束，未解除封榜
        /// </summary>
        Ended,

        /// <summary>
        /// 已结束
        /// </summary>
        Finalized,
    }

    /// <summary>
    /// 比赛
    /// </summary>
    public class Contest
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 比赛名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 比赛短名称
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// 比赛开始时间
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 榜冻结时间
        /// </summary>
        public DateTimeOffset? FreezeTime { get; set; }

        /// <summary>
        /// 比赛结束时间
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// 榜解封时间
        /// </summary>
        public DateTimeOffset? UnfreezeTime { get; set; }

        /// <summary>
        /// 排名方法
        /// </summary>
        public int RankingStrategy { get; set; }

        /// <summary>
        /// 是否公众可见
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// 默认注册分类
        /// </summary>
        public int RegisterDefaultCategory { get; set; }

        /// <summary>
        /// 打印可用性
        /// </summary>
        public bool PrintingAvaliable { get; set; }

        /// <summary>
        /// 气球可用性
        /// </summary>
        public bool BalloonAvaliable { get; set; }

        /// <summary>
        /// 标志位
        /// </summary>
        public bool Gym { get; set; }

        /// <summary>
        /// Gym下其他人代码可见性，0为不可见，1为可见，2为过题可见
        /// </summary>
        public int StatusAvaliable { get; set; }

        public ContestState GetState(DateTimeOffset? nowTime = null)
        {
            var now = nowTime ?? DateTimeOffset.Now;

            if (!StartTime.HasValue)
                return ContestState.NotScheduled;
            if (StartTime.Value > now)
                return ContestState.ScheduledToStart;
            if (!EndTime.HasValue)
                return ContestState.Started;

            if (FreezeTime.HasValue)
            {
                if (UnfreezeTime.HasValue && UnfreezeTime.Value < now)
                    return ContestState.Finalized;
                if (EndTime.Value < now)
                    return ContestState.Ended;
                if (FreezeTime.Value < now)
                    return ContestState.Frozen;
                return ContestState.Started;
            }
            else
            {
                if (EndTime.Value < now)
                    return ContestState.Finalized;
                return ContestState.Started;
            }
        }
    }
}
