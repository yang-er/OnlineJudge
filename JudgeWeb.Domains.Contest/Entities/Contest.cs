using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<Contest>
    {
        public void Configure(EntityTypeBuilder<Contest> entity)
        {
            entity.HasKey(e => e.ContestId);

            entity.Property(e => e.Name)
                .IsRequired();

            entity.Property(e => e.ShortName)
                .IsRequired();
        }
    }
}
