using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<TeamAffiliation>
    {
        public void Configure(EntityTypeBuilder<TeamAffiliation> entity)
        {
            entity.HasKey(e => e.AffiliationId);

            entity.HasIndex(e => e.ExternalId);

            entity.Property(e => e.CountryCode)
                .IsUnicode(false)
                .HasMaxLength(4);
        }
    }
}
