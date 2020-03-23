using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<SubmissionStatistics>
    {
        public void Configure(EntityTypeBuilder<SubmissionStatistics> entity)
        {
            entity.HasKey(e => new { e.Author, e.ContestId, e.ProblemId });

            entity.HasIndex(e => new { e.Author, e.ContestId });
            entity.HasIndex(e => e.ProblemId);

            entity.HasOne<Problem>()
                .WithMany()
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
