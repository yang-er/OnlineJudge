using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<ScoreCache>
    {
        public void Configure(EntityTypeBuilder<ScoreCache> entity)
        {
            entity.HasKey(e => new { e.ContestId, e.TeamId, e.ProblemId });

            entity.Property(e => e.FirstToSolve).HasDefaultValue(false);
            entity.Property(e => e.IsCorrectPublic).HasDefaultValue(false);
            entity.Property(e => e.IsCorrectRestricted).HasDefaultValue(false);
            entity.Property(e => e.SolveTimePublic).HasDefaultValue(0.0);
            entity.Property(e => e.SolveTimeRestricted).HasDefaultValue(0.0);
            entity.Property(e => e.PendingPublic).HasDefaultValue(0);
            entity.Property(e => e.PendingRestricted).HasDefaultValue(0);
            entity.Property(e => e.SubmissionPublic).HasDefaultValue(0);
            entity.Property(e => e.SubmissionRestricted).HasDefaultValue(0);
        }
    }
}
