using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<Balloon>
    {
        public void Configure(EntityTypeBuilder<Balloon> entity)
        {
            entity.HasKey(e => e.Id);

            entity.HasOne<Submission>(e => e.s)
                .WithMany()
                .HasForeignKey(e => e.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Done)
                .HasDefaultValue(false);

            entity.Ignore(e => e.BalloonColor);
            entity.Ignore(e => e.ProblemShortName);
            entity.Ignore(e => e.ProblemId);
            entity.Ignore(e => e.Team);
            entity.Ignore(e => e.CategoryName);
            entity.Ignore(e => e.FirstToSolve);
            entity.Ignore(e => e.Time);
            entity.Ignore(e => e.Location);
            entity.Ignore(e => e.SortOrder);
        }
    }
}
