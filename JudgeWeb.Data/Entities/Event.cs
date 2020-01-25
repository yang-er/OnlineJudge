using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    public class Event
    {
        /// <summary>
        /// 事件编号
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// 事件时间
        /// </summary>
        public DateTimeOffset EventTime { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 终结点类型
        /// </summary>
        public string EndPointType { get; set; }

        /// <summary>
        /// 终结点编号
        /// </summary>
        public string EndPointId { get; set; }

        /// <summary>
        /// 终结点操作
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 终结点内容
        /// </summary>
        public byte[] Content { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> entity)
        {
            entity.HasKey(e => e.EventId);

            entity.HasIndex(e => e.EventTime);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.EndPointType)
                .HasMaxLength(32)
                .IsRequired()
                .IsUnicode(false);

            entity.Property(e => e.EndPointId)
                .IsRequired();

            entity.Property(e => e.Action)
                .HasMaxLength(6)
                .IsRequired()
                .IsUnicode(false);

            entity.Property(e => e.Content)
                .HasMaxLength(2048)
                .IsRequired();
        }
    }
}
