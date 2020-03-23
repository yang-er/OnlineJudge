using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> entity)
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .HasMaxLength(32);

            entity.Property(s => s.Id)
                .ValueGeneratedNever();

            entity.Ignore(s => s.UserName);

            entity.Ignore(s => s.Email);

            entity.Ignore(s => s.UserId);

            entity.Ignore(s => s.IsVerified);
        }
    }
}
