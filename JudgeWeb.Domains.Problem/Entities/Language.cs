using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemEntityTypeConfiguration
        : IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> entity)
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(16);

            entity.Property(e => e.Name)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(32);

            entity.Property(e => e.FileExtension)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(32);

            entity.Property(e => e.CompileScript)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasOne<Executable>()
                .WithMany()
                .HasForeignKey(e => e.CompileScript)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
