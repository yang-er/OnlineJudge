using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemEntityTypeConfiguration
        : IEntityTypeConfiguration<Executable>
    {
        public void Configure(EntityTypeBuilder<Executable> entity)
        {
            entity.HasKey(e => e.ExecId);

            entity.Property(e => e.ExecId)
                .IsRequired()
                .HasMaxLength(64)
                .IsUnicode(false);

            entity.Property(e => e.Md5sum)
                .IsRequired()
                .HasMaxLength(32)
                .IsUnicode(false);

            entity.Property(e => e.ZipFile)
                .IsRequired()
                .HasMaxLength(1 << 20);
        }
    }
}
