using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 气球
    /// </summary>
    public class Balloon
    {
        /// <summary>
        /// 气球编号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 提交编号
        /// </summary>
        public int SubmissionId { get; set; }

        /// <summary>
        /// 是否已经分发出去
        /// </summary>
        public bool Done { get; set; }

        /// <summary>
        /// [Ignore] 气球颜色
        /// </summary>
        public string BalloonColor { get; set; }

        /// <summary>
        /// [Ignore] 题目短名
        /// </summary>
        public string ProblemShortName { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// [Ignore] 队伍
        /// </summary>
        public string Team { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// 是否首个解出题目
        /// </summary>
        public bool FirstToSolve { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 队伍位置
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 榜单排序类
        /// </summary>
        public int SortOrder { get; set; }

        public Balloon() { }

        public Balloon(Balloon b, int probid, int teamid, string teamName, string teamLoc, DateTimeOffset time, string catName, int so)
        {
            Id = b.Id;
            SubmissionId = b.SubmissionId;
            Done = b.Done;
            Team = $"t{teamid}: {teamName}";
            CategoryName = catName;
            Time = time;
            ProblemId = probid;
            Location = teamLoc;
            SortOrder = so;
        }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Balloon>
    {
        public void Configure(EntityTypeBuilder<Balloon> entity)
        {
            entity.HasKey(e => e.Id);

            entity.HasOne<Submission>()
                .WithMany()
                .HasForeignKey(e => e.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Done)
                .HasDefaultValue(false);

            entity.Ignore(e => e.BalloonColor);
            entity.Ignore(e => e.ProblemShortName);
            entity.Ignore(e => e.ProblemId);
            entity.Ignore(e => e.Team);
            entity.Ignore(e => e.CategoryName);
            entity.Ignore(e => e.FirstToSolve);
            entity.Ignore(e => e.Time);
            entity.Ignore(e => e.Location);
            entity.Ignore(e => e.SortOrder);
        }
    }
}
