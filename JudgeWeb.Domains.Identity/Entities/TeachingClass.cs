using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<TeachingClass>
    {
        public void Configure(EntityTypeBuilder<TeachingClass> entity)
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .HasMaxLength(64);

            entity.Ignore(s => s.Count);
        }
    }
}
