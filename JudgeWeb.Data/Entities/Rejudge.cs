using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 重判请求
    /// </summary>
    public class Rejudge
    {
        /// <summary>
        /// 重判编号
        /// </summary>
        public int RejudgeId { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 重判原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// 操作员
        /// </summary>
        public int? IssuedBy { get; set; }

        /// <summary>
        /// [Ignore] 操作者
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// 操作员
        /// </summary>
        public int? OperatedBy { get; set; }

        /// <summary>
        /// [Ignore] 操作者
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 是否已应用
        /// </summary>
        public bool? Applied { get; set; }

        /// <summary>
        /// [Ignore] 是否已准备好
        /// </summary>
        public (int, int) Ready { get; set; }

        public Rejudge() { }

        public Rejudge(Rejudge r1, string u1, string u2)
        {
            Applied = r1.Applied;
            RejudgeId = r1.RejudgeId;
            ContestId = r1.ContestId;
            IssuedBy = r1.IssuedBy;
            Issuer = u1;
            OperatedBy = r1.OperatedBy;
            Operator = u2;
            StartTime = r1.StartTime;
            EndTime = r1.EndTime;
            Reason = r1.Reason;
        }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Rejudge>
    {
        public void Configure(EntityTypeBuilder<Rejudge> entity)
        {
            entity.HasKey(e => e.RejudgeId);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Reason)
                .IsRequired();

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.IssuedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.OperatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(e => e.Issuer);
            entity.Ignore(e => e.Operator);
            entity.Ignore(e => e.Ready);
        }
    }
}
