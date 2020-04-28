using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<ContestProblem>
    {
        public void Configure(EntityTypeBuilder<ContestProblem> entity)
        {
            entity.HasKey(e => new { e.ContestId, e.ProblemId });

            entity.HasOne<Contest>(e => e.c)
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Problem>(e => e.p)
                .WithMany()
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ShortName)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasIndex(e => new { e.ContestId, e.ShortName })
                .IsUnique();

            entity.Property(e => e.Color)
                .IsRequired();

            entity.Ignore(e => e.Rank);
            entity.Ignore(e => e.Title);
            entity.Ignore(e => e.TimeLimit);
            entity.Ignore(e => e.MemoryLimit);
            entity.Ignore(e => e.TestcaseCount);
            entity.Ignore(e => e.Interactive);
            entity.Ignore(e => e.Shared);
            entity.Ignore(e => e.AllowJudge);
        }
    }
}
