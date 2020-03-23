using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> entity)
        {
            entity.HasOne<Problem>()
                .WithMany()
                .HasForeignKey(r => r.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Contest>()
                .WithMany()
                .HasForeignKey(r => r.ContestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new Role { Id = -1, ConcurrencyStamp = "17337d8e-0118-4c42-9da6-e4ca600d5836", Name = "Administrator", NormalizedName = "ADMINISTRATOR", ShortName = "admin", Description = "Administrative User" },
                new Role { Id = -2, ConcurrencyStamp = "9ec57e90-312c-4eed-ac25-a40fbcf5f33b", Name = "Blocked", NormalizedName = "BLOCKED", ShortName = "blocked", Description = "Blocked User" },
                new Role { Id = -3, ConcurrencyStamp = "2bbf420d-6253-4ace-a825-4bf8e85cf41e", Name = "Problem", NormalizedName = "PROBLEM", ShortName = "prob", Description = "Problem Provider" },
                new Role { Id = -4, ConcurrencyStamp = "fd0d1cf4-2ccf-4fd6-9d47-7fd62923c5d2", Name = "Judgehost", NormalizedName = "JUDGEHOST", ShortName = "judgehost", Description = "(Internal/System) Judgehost" },
                new Role { Id = -5, ConcurrencyStamp = "81ffd1be-883c-4093-8adf-f2a4909370b7", Name = "CDS", NormalizedName = "CDS", ShortName = "cds_api", Description = "CDS API user" },
                new Role { Id = -6, ConcurrencyStamp = "e5db5526-f7cd-41d5-aaf9-391b3ed17b3d", Name = "Student", NormalizedName = "STUDENT", ShortName = "stud", Description = "Verified Student" });
        }
    }
}
