using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<Judging>
    {
        public void Configure(EntityTypeBuilder<Judging> entity)
        {
            entity.HasKey(e => e.JudgingId);

            entity.HasOne<Submission>(e => e.s)
                .WithMany(e => e.Judgings)
                .HasForeignKey(e => e.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<JudgeHost>()
                .WithMany()
                .HasForeignKey(e => e.Server)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Server)
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.CompileError)
                .IsUnicode(false)
                .HasMaxLength(131072);

            entity.HasOne<Rejudge>()
                .WithMany()
                .HasForeignKey(e => e.RejudgeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Judging>()
                .WithMany()
                .HasForeignKey(e => e.PreviousJudgingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
