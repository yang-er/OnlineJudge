using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<TrainingTeamUser>
    {
        public void Configure(EntityTypeBuilder<TrainingTeamUser> entity)
        {
            entity.HasKey(e => new { e.TrainingTeamId, e.UserId });

            entity.HasOne<TrainingTeam>()
                .WithMany()
                .HasForeignKey(e => e.TrainingTeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.UserName);

            entity.Ignore(e => e.UserEmail);
        }
    }
}
