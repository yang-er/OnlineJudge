using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 可执行文件
    /// </summary>
    public class Executable
    {
        /// <summary>
        /// 程序编号
        /// </summary>
        public string ExecId { get; set; }

        /// <summary>
        /// 压缩包MD5
        /// </summary>
        public string Md5sum { get; set; }

        /// <summary>
        /// 压缩包，最大1MB
        /// </summary>
        public byte[] ZipFile { get; set; }

        /// <summary>
        /// 压缩包大小
        /// </summary>
        public int ZipSize { get; set; }

        /// <summary>
        /// 可执行文件描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 可执行文件类型
        /// </summary>
        public string Type { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Executable>
    {
        public void Configure(EntityTypeBuilder<Executable> entity)
        {
            entity.HasKey(e => e.ExecId);

            entity.Property(e => e.ExecId)
                .IsRequired()
                .HasMaxLength(64)
                .IsUnicode(false);

            entity.Property(e => e.Md5sum)
                .IsRequired()
                .HasMaxLength(32)
                .IsUnicode(false);

            entity.Property(e => e.ZipFile)
                .IsRequired()
                .HasMaxLength(1 << 20);
        }
    }
}
