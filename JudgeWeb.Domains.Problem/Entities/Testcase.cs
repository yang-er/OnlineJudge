using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemEntityTypeConfiguration
        : IEntityTypeConfiguration<Testcase>
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
