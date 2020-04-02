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

            entity.HasAlternateKey(e => e.ProblemId);

            entity.HasOne<Problem>(e => e.p)
                .WithOne(p => p.Archive)
                .HasForeignKey<ProblemArchive>(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.TagName)
                .IsRequired();

            entity.Ignore(e => e.Title);

            entity.Ignore(e => e.Source);

            entity.Ignore(e => e.Submitted);

            entity.Ignore(e => e.AllowSubmit);
        }
    }
}
