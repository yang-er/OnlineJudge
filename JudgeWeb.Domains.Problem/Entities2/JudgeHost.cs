using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<JudgeHost>
    {
        public void Configure(EntityTypeBuilder<JudgeHost> entity)
        {
            entity.HasKey(e => e.ServerName);

            entity.Property(e => e.ServerName)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(64);
        }
    }
}
