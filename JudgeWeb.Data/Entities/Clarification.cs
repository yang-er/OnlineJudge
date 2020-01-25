using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 提问
    /// </summary>
    public class Clarification
    {
        /// <summary>
        /// 提问编号
        /// </summary>
        public int ClarificationId { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 回应的提问编号
        /// </summary>
        public int? ResponseToId { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTimeOffset SubmitTime { get; set; }

        /// <summary>
        /// 发者队伍编号
        /// </summary>
        public int? Sender { get; set; }

        /// <summary>
        /// 收者队伍编号
        /// </summary>
        public int? Recipient { get; set; }

        /// <summary>
        /// 裁判组成员
        /// </summary>
        public string JuryMember { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int? ProblemId { get; set; }

        /// <summary>
        /// 提问分类
        /// </summary>
        public TargetCategory Category { get; set; }

        /// <summary>
        /// 提问正文
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 是否已经被回答
        /// </summary>
        public bool Answered { get; set; }

        /// <summary>
        /// [Ignore] 队伍名称
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 检查是否有权限
        /// </summary>
        /// <param name="teamid">队伍编号</param>
        public bool CheckPermission(int teamid)
        {
            return !Recipient.HasValue || Recipient == teamid || Sender == teamid;
        }

        /// <summary>
        /// 提问分类
        /// </summary>
        public enum TargetCategory
        {
            General,
            Technical,
            Problem
        }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Clarification>
    {
        public void Configure(EntityTypeBuilder<Clarification> entity)
        {
            entity.HasKey(e => e.ClarificationId);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Clarification>()
                .WithMany()
                .HasForeignKey(e => e.ResponseToId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ResponseToId);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(e => new { e.ContestId, TeamId = e.Sender })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Recipient);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(e => new { e.ContestId, TeamId = e.Recipient })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Problem>()
                .WithMany()
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Body)
                .IsRequired();

            entity.Ignore(e => e.TeamName);
        }
    }
}
