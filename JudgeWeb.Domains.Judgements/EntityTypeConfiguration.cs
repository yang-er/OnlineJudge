using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Domains.Judgements
{
    public static class EntityTypeConfiguration
    {
        public static void ApplyJudgementDomain(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Detail>(entity =>
            {
                entity.HasKey(e => e.TestId);

                entity.HasOne<Judging>()
                    .WithMany(e => e.Details)
                    .HasForeignKey(e => e.JudgingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Testcase>()
                    .WithMany()
                    .HasForeignKey(e => e.TestcaseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.MetaData)
                    .IsUnicode(false)
                    .HasMaxLength(131072);

                entity.Property(e => e.OutputSystem)
                    .IsUnicode(false)
                    .HasMaxLength(131072);

                entity.Property(e => e.OutputDiff)
                    .IsUnicode(false)
                    .HasMaxLength(131072);
            });

            modelBuilder.Entity<InternalError>(entity =>
            {
                entity.HasKey(e => e.ErrorId);

                entity.HasIndex(e => e.Status);

                entity.Property(e => e.Description)
                    .IsRequired();

                entity.Property(e => e.Disabled)
                    .IsRequired();

                entity.Property(e => e.JudgehostLog)
                    .IsRequired();
            });

            modelBuilder.Entity<JudgeHost>(entity =>
            {
                entity.HasKey(e => e.ServerName);

                entity.Property(e => e.ServerName)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<Rejudge>(entity =>
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
            });

            modelBuilder.Entity<Judging>(entity =>
            {
                entity.HasKey(e => e.JudgingId);

                entity.HasOne<Submission>()
                    .WithMany(e => e.Judgings)
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<JudgeHost>()
                    .WithMany()
                    .HasForeignKey(e => e.Server)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Server)
                    .IsUnicode(false)
                    .HasMaxLength(64);

                entity.HasIndex(e => e.Status);

                entity.Property(e => e.CompileError)
                    .IsUnicode(false)
                    .HasMaxLength(131072);

                entity.HasOne<Rejudge>()
                    .WithMany()
                    .HasForeignKey(e => e.RejudgeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Judging>()
                    .WithMany()
                    .HasForeignKey(e => e.PreviousJudgingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SubmissionStatistics>(entity =>
            {
                entity.HasKey(e => new { e.Author, e.ContestId, e.ProblemId });

                entity.HasIndex(e => new { e.Author, e.ContestId });
                entity.HasIndex(e => e.ProblemId);

                entity.HasOne<Problem>()
                    .WithMany()
                    .HasForeignKey(e => e.ProblemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Auditlog>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.HasOne<Contest>()
                    .WithMany()
                    .HasForeignKey(e => e.ContestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.UserName)
                    .IsRequired();

                entity.HasIndex(e => e.DataType);

                entity.Property(e => e.DataId)
                    .HasMaxLength(256)
                    .IsUnicode(false);

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(256)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Configure>(entity =>
            {
                entity.HasKey(e => e.ConfigId);

                entity.HasIndex(e => e.Name);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .IsUnicode(false)
                    .HasMaxLength(128);

                entity.Property(e => e.Value)
                    .IsRequired()
                    .IsUnicode(false);

                entity.HasData(
                    new Configure { ConfigId = -1, Category = "Judging", Name = "process_limit", Value = "64", Type = "int", Description = "Maximum number of processes that the submission is allowed to start (including shell and possibly interpreters).", Public = 0 },
                    new Configure { ConfigId = -2, Category = "Judging", Name = "script_timelimit", Value = "30", Type = "int", Description = "Maximum seconds available for compile/compare scripts. This is a safeguard against malicious code and buggy scripts, so a reasonable but large amount should do.", Public = 0 },
                    new Configure { ConfigId = -3, Category = "Judging", Name = "script_memory_limit", Value = "2097152", Type = "int", Description = "Maximum memory usage (in kB) by compile/compare scripts. This is a safeguard against malicious code and buggy script, so a reasonable but large amount should do.", Public = 0 },
                    new Configure { ConfigId = -4, Category = "Judging", Name = "script_filesize_limit", Value = "540672", Type = "int", Description = "Maximum filesize (in kB) compile/compare scripts may write. Submission will fail with compiler-error when trying to write more, so this should be greater than any *intermediate or final* result written by compilers.", Public = 0 },
                    new Configure { ConfigId = -5, Category = "Judging", Name = "timelimit_overshoot", Value = "\"1s|10%\"", Type = "string", Description = "Time that submissions are kept running beyond timelimit before being killed. Specify as \"Xs\" for X seconds, \"Y%\" as percentage, or a combination of both separated by one of \"+|&\" for the sum, maximum, or minimum of both.", Public = 0 },
                    new Configure { ConfigId = -6, Category = "Judging", Name = "output_storage_limit", Value = "60000", Type = "int", Description = "Maximum size of error/system output stored in the database (in bytes); use \"-1\" to disable any limits.", Public = 0 },
                    new Configure { ConfigId = -7, Category = "Judging", Name = "diskspace_error", Value = "1048576", Type = "int", Description = "Minimum free disk space (in kB) on judgehosts.", Public = 0 },
                    new Configure { ConfigId = -8, Category = "Judging", Name = "update_judging_seconds", Value = "0", Type = "int", Description = "Post updates to a judging every X seconds. Set to 0 to update after each judging_run.", Public = 0 });
            });
        }
    }
}
