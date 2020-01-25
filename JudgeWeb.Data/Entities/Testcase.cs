using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 测试用例
    /// </summary>
    public class Testcase
    {
        /// <summary>
        /// 用例编号
        /// </summary>
        public int TestcaseId { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 是否为保密数据
        /// </summary>
        public bool IsSecret { get; set; }

        /// <summary>
        /// 输入的MD5
        /// </summary>
        public string Md5sumInput { get; set; }

        /// <summary>
        /// 输出的MD5
        /// </summary>
        public string Md5sumOutput { get; set; }

        /// <summary>
        /// 输入长度
        /// </summary>
        public int InputLength { get; set; }

        /// <summary>
        /// 输出长度
        /// </summary>
        public int OutputLength { get; set; }

        /// <summary>
        /// 用例顺序
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 测试点分数
        /// </summary>
        public int Point { get; set; }

        /// <summary>
        /// 测试点描述
        /// </summary>
        public string Description { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Testcase>
    {
        public void Configure(EntityTypeBuilder<Testcase> entity)
        {
            entity.HasKey(e => e.TestcaseId);

            entity.HasIndex(e => e.ProblemId);

            entity.HasOne<Problem>()
                .WithMany()
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Md5sumInput)
                .HasMaxLength(32)
                .IsRequired()
                .IsUnicode(false);

            entity.Property(e => e.Md5sumOutput)
                .HasMaxLength(32)
                .IsRequired()
                .IsUnicode(false);

            entity.Property(e => e.Description)
                .HasMaxLength(1 << 9)
                .IsRequired();
        }
    }
}
