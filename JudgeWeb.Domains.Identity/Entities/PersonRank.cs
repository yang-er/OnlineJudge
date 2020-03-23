using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<PersonRank>
    {
        public void Configure(EntityTypeBuilder<PersonRank> entity)
        {
            entity.HasKey(r => r.Id);

            entity.HasIndex(r => r.Grade);

            entity.Property(r => r.Account)
                .IsRequired();

            entity.Property(r => r.ACMer)
                .IsRequired();
        }
    }
}
