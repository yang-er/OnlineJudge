using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<Clarification>
    {
        public void Configure(EntityTypeBuilder<Clarification> entity)
        {
            entity.HasKey(e => e.ClarificationId);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Clarification>()
                .WithMany()
                .HasForeignKey(e => e.ResponseToId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ResponseToId);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(e => new { e.ContestId, TeamId = e.Sender })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Recipient);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(e => new { e.ContestId, TeamId = e.Recipient })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Problem>()
                .WithMany()
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Body)
                .IsRequired();

            entity.Ignore(e => e.TeamName);
        }
    }
}
