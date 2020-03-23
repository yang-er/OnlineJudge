using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<TeamCategory>
    {
        public void Configure(EntityTypeBuilder<TeamCategory> entity)
        {
            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.Name)
                .IsRequired();

            entity.Property(e => e.Color)
                .IsRequired();

            entity.HasIndex(e => e.SortOrder);

            entity.HasIndex(e => e.IsPublic);
        }
    }
}
