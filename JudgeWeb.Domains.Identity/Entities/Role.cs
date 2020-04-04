using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq;
using System.Security.Claims;

namespace JudgeWeb.Domains.Identity
{
    public partial class IdentityEntityTypeConfiguration :
        IEntityTypeConfiguration<Role>,
        IEntityTypeConfiguration<IdentityRoleClaim<int>>
    {
        public static readonly Role[] HasRoles = new[]
        {
            new Role { Id = -1, ConcurrencyStamp = "17337d8e-0118-4c42-9da6-e4ca600d5836", Name = "Administrator", NormalizedName = "ADMINISTRATOR", ShortName = "admin", Description = "Administrative User" },
            new Role { Id = -2, ConcurrencyStamp = "9ec57e90-312c-4eed-ac25-a40fbcf5f33b", Name = "Blocked", NormalizedName = "BLOCKED", ShortName = "blocked", Description = "Blocked User" },
            new Role { Id = -3, ConcurrencyStamp = "2bbf420d-6253-4ace-a825-4bf8e85cf41e", Name = "Problem", NormalizedName = "PROBLEM", ShortName = "prob", Description = "Problem Provider" },
            new Role { Id = -4, ConcurrencyStamp = "fd0d1cf4-2ccf-4fd6-9d47-7fd62923c5d2", Name = "Judgehost", NormalizedName = "JUDGEHOST", ShortName = "judgehost", Description = "(Internal/System) Judgehost" },
            new Role { Id = -5, ConcurrencyStamp = "81ffd1be-883c-4093-8adf-f2a4909370b7", Name = "CDS", NormalizedName = "CDS", ShortName = "cds_api", Description = "CDS API user" },
            new Role { Id = -6, ConcurrencyStamp = "e5db5526-f7cd-41d5-aaf9-391b3ed17b3d", Name = "Student", NormalizedName = "STUDENT", ShortName = "stud", Description = "Verified Student" },
            new Role { Id = -7, ConcurrencyStamp = "9fa1ba39-2249-49e0-8378-ac041b89624d", Name = "Teacher", NormalizedName = "TEACHER", ShortName = "teach", Description = "Teacher" }
        };

        public static int OfRole(string role) => HasRoles.Single(r => r.Name == role).Id;

        public static int[] OfRoles(params string[] roles) => roles.Select(r => OfRole(r)).ToArray();

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

            entity.HasData(HasRoles);
        }

        public static readonly (Claim, int[])[] RoleClaims = new[]
        {
            (new Claim("create_contest", "true"), OfRoles("Administrator")),
            (new Claim("create_problem", "true"), OfRoles("Administrator", "Problem", "Teacher")),
            (new Claim("manage_stud_group", "true"), OfRoles("Administrator", "Teacher")),
            (new Claim("plag_detect", "true"), OfRoles("Administrator", "Teacher")),
            (new Claim("judger", "true"), OfRoles("Judgehost")),
            (new Claim("read_contest", "true"), OfRoles("Administrator", "CDS")),
            (new Claim("rank_list", "true"), OfRoles("Administrator", "Teacher", "Student")),
        };

        public void Configure(EntityTypeBuilder<IdentityRoleClaim<int>> entity)
        {
            entity.HasData(RoleClaims
                .SelectMany(
                    collectionSelector: c => c.Item2,
                    resultSelector: (c, i) => new { claim = c.Item1, roleId = i })
                .Select(
                    selector: (a, i) => new IdentityRoleClaim<int>
                    {
                        Id = -1 - i,
                        RoleId = a.roleId,
                        ClaimType = a.claim.Type,
                        ClaimValue = a.claim.Value
                    }));
        }
    }
}
