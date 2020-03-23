using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Problems
{
    public partial class JudgementEntityTypeConfiguration
        : IEntityTypeConfiguration<Rejudge>
    {
        public void Configure(EntityTypeBuilder<Rejudge> entity)
        {
            entity.HasKey(e => e.RejudgeId);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(e => e.ContestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Reason)
                .IsRequired();

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.IssuedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.OperatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(e => e.Issuer);
            entity.Ignore(e => e.Operator);
            entity.Ignore(e => e.Ready);
        }
    }
}
