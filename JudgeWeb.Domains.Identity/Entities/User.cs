using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration :
        IEntityTypeConfiguration<User>,
        IEntityTypeConfiguration<IdentityUserRole<int>>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.HasData(
                new User
                {
                    Id = -1,
                    UserName = "judgehost",
                    NormalizedUserName = "JUDGEHOST",
                    Email = "acm@xylab.fun",
                    NormalizedEmail = "ACM@XYLAB.FUN",
                    EmailConfirmed = true,
                    ConcurrencyStamp = "e1a1189a-38f5-487b-907b-6d0533722f02",
                    SecurityStamp = "AAAABBBBCCCCDDDDEEEEFFFFGGGGHHHH",
                    LockoutEnabled = false,
                    NickName = "User for judgedaemons"
                });

            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(u => u.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(u => u.NickName)
                .HasMaxLength(256);
        }

        public void Configure(EntityTypeBuilder<IdentityUserRole<int>> entity)
        {
            entity.HasData(
                new IdentityUserRole<int> { RoleId = OfRole("Judgehost"), UserId = -1 });
        }
    }
}
