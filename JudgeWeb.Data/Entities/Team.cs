using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 比赛队伍
    /// </summary>
    public class Team
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
        public int? UserId { get; set; }

        /// <summary>
        /// 队伍名称
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 归属组织编号
        /// </summary>
        public int AffiliationId { get; set; }

        /// <summary>
        /// 比赛分类编号
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// 队伍状态，0为挂起，1为通过，2为拒绝，3为已删除
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTimeOffset? RegisterTime { get; set; }

        /// <summary>
        /// 队伍位置
        /// </summary>
        public string Location { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> entity)
        {
            entity.HasKey(e => new { e.ContestId, e.TeamId });

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.TeamName)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasOne<TeamAffiliation>()
                .WithMany()
                .HasForeignKey(e => e.AffiliationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<TeamCategory>()
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Status);
        }
    }
}
