using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<InternalError>
    {
        public void Configure(EntityTypeBuilder<InternalError> entity)
        {
            entity.HasKey(e => e.ErrorId);

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Description)
                .IsRequired();

            entity.Property(e => e.Disabled)
                .IsRequired();

            entity.Property(e => e.JudgehostLog)
                .IsRequired();
        }
    }
}
