using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 内部错误状态
    /// </summary>
    public enum InternalErrorStatus
    {
        /// <summary>
        /// 未解决
        /// </summary>
        Open,

        /// <summary>
        /// 已解决
        /// </summary>
        Resolved,

        /// <summary>
        /// 已忽略
        /// </summary>
        Ignored
    }

    /// <summary>
    /// 评测机错误
    /// </summary>
    public class InternalError
    {
        /// <summary>
        /// 错误编号
        /// </summary>
        public int ErrorId { get; set; }

        /// <summary>
        /// 比赛编号
        /// </summary>
        public int? ContestId { get; set; }

        /// <summary>
        /// 评测编号
        /// </summary>
        public int? JudgingId { get; set; }

        /// <summary>
        /// 错误描述，BASE64
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 评测机日志，BASE64
        /// </summary>
        public string JudgehostLog { get; set; }

        /// <summary>
        /// 查看时间
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 禁用的对象
        /// </summary>
        public string Disabled { get; set; }

        /// <summary>
        /// 错误状态
        /// </summary>
        public InternalErrorStatus Status { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<InternalError>
    {
        public void Configure(EntityTypeBuilder<InternalError> entity)
        {
            entity.HasKey(e => e.ErrorId);

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Description)
                .IsRequired();

            entity.Property(e => e.Disabled)
                .IsRequired();

            entity.Property(e => e.JudgehostLog)
                .IsRequired();
        }
    }
}
