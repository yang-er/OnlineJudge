using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

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

        /// <summary>
        /// 归属组织
        /// </summary>
        public TeamAffiliation Affiliation { get; }

        /// <summary>
        /// 排名缓存
        /// </summary>
        public ICollection<RankCache> RankCache { get; set; }

        /// <summary>
        /// 分数缓存
        /// </summary>
        public ICollection<ScoreCache> ScoreCache { get; set; }
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

            entity.Property(e => e.TeamName)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasOne<TeamAffiliation>(e => e.Affiliation)
                .WithMany()
                .HasForeignKey(e => e.AffiliationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<TeamCategory>()
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ScoreCache)
                .WithOne()
                .HasForeignKey(sc => new { sc.ContestId, sc.TeamId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RankCache)
                .WithOne()
                .HasForeignKey(rc => new { rc.ContestId, rc.TeamId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Status);
        }
    }
}
