using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemEntityTypeConfiguration
        : IEntityTypeConfiguration<Problem>
    {
        public void Configure(EntityTypeBuilder<Problem> entity)
        {
            entity.HasKey(e => e.ProblemId);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.RunScript)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasOne<Executable>()
                .WithMany()
                .HasForeignKey(e => e.RunScript)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CompareScript)
                .IsRequired()
                .IsUnicode(false)
                .HasMaxLength(64);

            entity.HasOne<Executable>()
                .WithMany()
                .HasForeignKey(e => e.CompareScript)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ComapreArguments)
                .IsUnicode(false)
                .HasMaxLength(128);
        }
    }
}
