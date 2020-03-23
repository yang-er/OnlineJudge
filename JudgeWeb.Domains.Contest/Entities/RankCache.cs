using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<RankCache>
    {
        public void Configure(EntityTypeBuilder<RankCache> entity)
        {
            entity.HasKey(e => new { e.ContestId, e.TeamId });

            entity.Property(e => e.PointsPublic).HasDefaultValue(0);
            entity.Property(e => e.PointsRestricted).HasDefaultValue(0);
            entity.Property(e => e.TotalTimeRestricted).HasDefaultValue(0);
            entity.Property(e => e.TotalTimePublic).HasDefaultValue(0);
        }
    }
}
