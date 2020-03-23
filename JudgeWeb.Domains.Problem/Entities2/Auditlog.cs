using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<Auditlog>
    {
        public void Configure(EntityTypeBuilder<Auditlog> entity)
        {
            entity.HasKey(e => e.LogId);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.UserName)
                .IsRequired();

            entity.HasIndex(e => e.DataType);

            entity.Property(e => e.DataId)
                .HasMaxLength(256)
                .IsUnicode(false);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(256)
                .IsUnicode(false);
        }
    }
}
