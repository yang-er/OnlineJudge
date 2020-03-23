using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<Submission>
    {
        public void Configure(EntityTypeBuilder<Submission> entity)
        {
            entity.HasKey(e => e.SubmissionId);

            entity.HasIndex(e => e.ContestId);
            entity.HasIndex(e => new { e.Author, e.ContestId });
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.ProblemId);
            entity.HasIndex(e => e.RejudgeId);

            entity.HasOne<Problem>(e => e.p)
                .WithMany()
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.SourceCode)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(131072);

            entity.HasOne<Language>(e => e.l)
                .WithMany()
                .HasForeignKey(e => e.Language)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Language)
                .IsRequired()
                .HasMaxLength(16)
                .IsUnicode(false);

            entity.Property(e => e.Ip)
                .IsRequired()
                .HasMaxLength(128)
                .IsUnicode(false);

            entity.HasOne<Rejudge>()
                .WithMany()
                .HasForeignKey(e => e.RejudgeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
