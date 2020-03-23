using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<TeamMember>
    {
        public void Configure(EntityTypeBuilder<TeamMember> entity)
        {
            entity.HasKey(e => new { e.ContestId, e.TeamId, e.UserId });

            entity.HasOne<Team>(e => e.Team)
                .WithMany()
                .HasForeignKey(e => new { e.ContestId, e.TeamId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ContestId, e.UserId })
                .IsUnique();
        }
    }
}
