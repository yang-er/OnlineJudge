using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<Printing>
    {
        public void Configure(EntityTypeBuilder<Printing> entity)
        {
            entity.HasKey(e => e.Id);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.SourceCode)
                .IsRequired()
                .HasMaxLength(65536);

            entity.Property(e => e.FileName)
                .HasMaxLength(256)
                .IsUnicode(false);

            entity.Property(e => e.LanguageId)
                .IsUnicode(false)
                .HasMaxLength(10);
        }
    }
}
