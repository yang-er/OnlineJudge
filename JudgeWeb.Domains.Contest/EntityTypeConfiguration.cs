using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Domains.Contests
{
    public static class EntityTypeConfiguration
    {
        public static void ApplyContestDomain(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Balloon>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne<Submission>()
                    .WithMany()
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Done)
                    .HasDefaultValue(false);

                entity.Ignore(e => e.BalloonColor);
                entity.Ignore(e => e.ProblemShortName);
                entity.Ignore(e => e.ProblemId);
                entity.Ignore(e => e.Team);
                entity.Ignore(e => e.CategoryName);
                entity.Ignore(e => e.FirstToSolve);
                entity.Ignore(e => e.Time);
                entity.Ignore(e => e.Location);
                entity.Ignore(e => e.SortOrder);
            });

            modelBuilder.Entity<Clarification>(entity =>
            {
                entity.HasKey(e => e.ClarificationId);

                entity.HasOne<Contest>()
                    .WithMany()
                    .HasForeignKey(e => e.ContestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Clarification>()
                    .WithMany()
                    .HasForeignKey(e => e.ResponseToId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ResponseToId);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(e => new { e.ContestId, TeamId = e.Sender })
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Recipient);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(e => new { e.ContestId, TeamId = e.Recipient })
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Problem>()
                    .WithMany()
                    .HasForeignKey(e => e.ProblemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Body)
                    .IsRequired();

                entity.Ignore(e => e.TeamName);
            });

            modelBuilder.Entity<Contest>(entity =>
            {
                entity.HasKey(e => e.ContestId);

                entity.Property(e => e.Name)
                    .IsRequired();

                entity.Property(e => e.ShortName)
                    .IsRequired();
            });

            modelBuilder.Entity<ContestProblem>(entity =>
            {
                entity.HasKey(e => new { e.ContestId, e.ProblemId });

                entity.HasOne<Contest>()
                    .WithMany()
                    .HasForeignKey(e => e.ContestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Problem>()
                    .WithMany()
                    .HasForeignKey(e => e.ProblemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.ShortName)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.Color)
                    .IsRequired();

                entity.Ignore(e => e.Rank);
                entity.Ignore(e => e.Title);
                entity.Ignore(e => e.TimeLimit);
                entity.Ignore(e => e.MemoryLimit);
                entity.Ignore(e => e.TestcaseCount);
                entity.Ignore(e => e.Interactive);
                entity.Ignore(e => e.Shared);
            });

            modelBuilder.Entity<Event>(entity =>
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
            });

            modelBuilder.Entity<Printing>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne<Contest>()
                    .WithMany()
                    .HasForeignKey(e => e.ContestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.SourceCode)
                    .IsRequired()
                    .HasMaxLength(65536);

                entity.Property(e => e.FileName)
                    .HasMaxLength(256)
                    .IsUnicode(false);

                entity.Property(e => e.LanguageId)
                    .IsUnicode(false)
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => new { e.ContestId, e.TeamId });

                entity.HasOne<Contest>()
                    .WithMany()
                    .HasForeignKey(e => e.ContestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.TeamName)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.HasOne<TeamAffiliation>(e => e.Affiliation)
                    .WithMany()
                    .HasForeignKey(e => e.AffiliationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<TeamCategory>()
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.ScoreCache)
                    .WithOne()
                    .HasForeignKey(sc => new { sc.ContestId, sc.TeamId })
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.RankCache)
                    .WithOne()
                    .HasForeignKey(rc => new { rc.ContestId, rc.TeamId })
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.HasKey(e => new { e.ContestId, e.TeamId, e.UserId });

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(e => new { e.ContestId, e.TeamId })
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ContestId, e.UserId })
                    .IsUnique();
            });

            modelBuilder.Entity<Submission>(entity =>
            {
                entity.HasKey(e => e.SubmissionId);

                entity.HasIndex(e => e.ContestId);
                entity.HasIndex(e => e.Author);
                entity.HasIndex(e => e.Language);
                entity.HasIndex(e => e.ProblemId);
                entity.HasIndex(e => e.RejudgeId);

                entity.HasOne<Problem>()
                    .WithMany()
                    .HasForeignKey(e => e.ProblemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.SourceCode)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(131072);

                entity.HasOne<Language>()
                    .WithMany()
                    .HasForeignKey(e => e.Language)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Language)
                    .IsRequired()
                    .HasMaxLength(16)
                    .IsUnicode(false);

                entity.Property(e => e.Ip)
                    .IsRequired()
                    .HasMaxLength(128)
                    .IsUnicode(false);

                entity.HasOne<Rejudge>()
                    .WithMany()
                    .HasForeignKey(e => e.RejudgeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RankCache>(entity =>
            {
                entity.HasKey(e => new { e.ContestId, e.TeamId });

                entity.Property(e => e.PointsPublic).HasDefaultValue(0);
                entity.Property(e => e.PointsRestricted).HasDefaultValue(0);
                entity.Property(e => e.TotalTimeRestricted).HasDefaultValue(0);
                entity.Property(e => e.TotalTimePublic).HasDefaultValue(0);
            });

            modelBuilder.Entity<ScoreCache>(entity =>
            {
                entity.HasKey(e => new { e.ContestId, e.TeamId, e.ProblemId });

                entity.Property(e => e.FirstToSolve).HasDefaultValue(false);
                entity.Property(e => e.IsCorrectPublic).HasDefaultValue(false);
                entity.Property(e => e.IsCorrectRestricted).HasDefaultValue(false);
                entity.Property(e => e.SolveTimePublic).HasDefaultValue(0.0);
                entity.Property(e => e.SolveTimeRestricted).HasDefaultValue(0.0);
                entity.Property(e => e.PendingPublic).HasDefaultValue(0);
                entity.Property(e => e.PendingRestricted).HasDefaultValue(0);
                entity.Property(e => e.SubmissionPublic).HasDefaultValue(0);
                entity.Property(e => e.SubmissionRestricted).HasDefaultValue(0);
            });
        }
    }
}
