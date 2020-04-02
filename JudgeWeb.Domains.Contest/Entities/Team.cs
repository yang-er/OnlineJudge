using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> entity)
        {
            entity.HasKey(e => new { e.ContestId, e.TeamId });

            entity.HasOne<Contest>(e => e.Contest)
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.TeamName)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasOne<TeamAffiliation>(e => e.Affiliation)
                .WithMany()
                .HasForeignKey(e => e.AffiliationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<TeamCategory>(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany<ScoreCache>(e => e.ScoreCache)
                .WithOne()
                .HasForeignKey(sc => new { sc.ContestId, sc.TeamId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<RankCache>(e => e.rc)
                .WithOne()
                .HasForeignKey<RankCache>(rc => new { rc.ContestId, rc.TeamId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Status);
        }
    }
}
