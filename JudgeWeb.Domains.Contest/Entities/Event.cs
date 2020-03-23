using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Contests
{
    public partial class ContestEntityTypeConfiguration
        : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> entity)
        {
            entity.HasKey(e => e.EventId);

            entity.HasIndex(e => e.EventTime);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.EndPointType)
                .HasMaxLength(32)
                .IsRequired()
                .IsUnicode(false);

            entity.Property(e => e.EndPointId)
                .IsRequired();

            entity.Property(e => e.Action)
                .HasMaxLength(6)
                .IsRequired()
                .IsUnicode(false);

            entity.Property(e => e.Content)
                .HasMaxLength(2048)
                .IsRequired();
        }
    }
}
