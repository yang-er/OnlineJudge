using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemEntityTypeConfiguration
        : IEntityTypeConfiguration<ProblemArchive>
    {
        public void Configure(EntityTypeBuilder<ProblemArchive> entity)
        {
            entity.HasKey(e => e.PublicId);

            entity.Property(e => e.PublicId)
                .ValueGeneratedNever();

            entity.HasIndex(e => e.ProblemId);

            entity.HasOne<Problem>()
                .WithMany(p => p.ArchiveCollection)
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.TagName)
                .IsRequired();

            entity.Ignore(e => e.Title);

            entity.Ignore(e => e.Source);

            entity.Ignore(e => e.Submitted);
        }
    }
}
