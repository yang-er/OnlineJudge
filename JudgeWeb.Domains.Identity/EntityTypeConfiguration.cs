using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Domains.Identity
{
    public static class EntityTypeConfiguration
    {
        public static void ApplyIdentityDomain(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassStudent>(entity =>
            {
                entity.HasKey(e => new { e.StudentId, e.ClassId });

                entity.HasOne<Student>()
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<TeachingClass>()
                    .WithMany()
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<News>(entity =>
            {
                entity.HasKey(n => n.NewsId);
            });

            modelBuilder.Entity<PersonRank>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasIndex(r => r.Grade);

                entity.Property(r => r.Account)
                    .IsRequired();

                entity.Property(r => r.ACMer)
                    .IsRequired();
            });

            modelBuilder.Entity<Role>(entity =>
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
            });

            modelBuilder.Entity<Student>(entity =>
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
            });

            modelBuilder.Entity<TeachingClass>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                    .HasMaxLength(64);

                entity.Ignore(s => s.Count);
            });

            modelBuilder.Entity<TeamAffiliation>(entity =>
            {
                entity.HasKey(e => e.AffiliationId);

                entity.HasIndex(e => e.ExternalId);

                entity.Property(e => e.CountryCode)
                    .IsUnicode(false)
                    .HasMaxLength(4);
            });

            modelBuilder.Entity<TeamCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryId);

                entity.Property(e => e.Name)
                    .IsRequired();

                entity.Property(e => e.Color)
                    .IsRequired();

                entity.HasIndex(e => e.SortOrder);

                entity.HasIndex(e => e.IsPublic);
            });

            modelBuilder.Entity<TrainingTeam>(entity =>
            {
                entity.HasKey(e => e.TrainingTeamId);

                entity.Property(e => e.TeamName)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.HasOne<TeamAffiliation>(e => e.Affiliation)
                    .WithMany()
                    .HasForeignKey(e => e.AffiliationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TrainingTeamUser>(entity =>
            {
                entity.HasKey(e => new { e.TrainingTeamId, e.UserId });

                entity.HasOne<TrainingTeam>()
                    .WithMany()
                    .HasForeignKey(e => e.TrainingTeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Ignore(e => e.UserName);

                entity.Ignore(e => e.UserEmail);
            });

            modelBuilder.Entity<User>(entity =>
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
            });
        }
    }
}
