using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<Detail>
    {
        public void Configure(EntityTypeBuilder<Detail> entity)
        {
            entity.HasKey(e => e.TestId);

            entity.HasOne<Judging>()
                .WithMany(e => e.Details)
                .HasForeignKey(e => e.JudgingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Testcase>()
                .WithMany()
                .HasForeignKey(e => e.TestcaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.MetaData)
                .IsUnicode(false)
                .HasMaxLength(131072);

            entity.Property(e => e.OutputSystem)
                .IsUnicode(false)
                .HasMaxLength(131072);

            entity.Property(e => e.OutputDiff)
                .IsUnicode(false)
                .HasMaxLength(131072);
        }
    }
}
