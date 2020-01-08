using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    public class AppDbContext : IdentityDbContext<User, Role, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }


        public static event System.Action<ScoreboardState> ScoreboardUpdate;


        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<Submission> Submissions { get; set; }
        public virtual DbSet<Problem> Problems { get; set; }
        public virtual DbSet<ProblemArchive> Archives { get; set; }
        public virtual DbSet<Detail> Details { get; set; }
        public virtual DbSet<Judging> Judgings { get; set; }
        public virtual DbSet<Rejudge> Rejudges { get; set; }
        public virtual DbSet<News> News { get; set; }
        public virtual DbSet<Student> Students { get; set; }
        public virtual DbSet<PersonRank> PersonRanks { get; set; }


        public virtual DbSet<Contest> Contests { get; set; }
        public virtual DbSet<ContestProblem> ContestProblem { get; set; }
        public virtual DbSet<Clarification> Clarifications { get; set; }
        public virtual DbSet<Team> Teams { get; set; }
        public virtual DbSet<TeamAffiliation> TeamAffiliations { get; set; }
        public virtual DbSet<TeamCategory> TeamCategories { get; set; }
        public virtual DbSet<RankCache> RankCache { get; set; }
        public virtual DbSet<ScoreCache> ScoreCache { get; set; }
        public virtual DbSet<Event> Events { get; set; }


        public virtual DbSet<Configure> Configures { get; set; }
        public virtual DbSet<Executable> Executable { get; set; }
        public virtual DbSet<InternalError> InternalErrors { get; set; }
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<JudgeHost> JudgeHosts { get; set; }
        public virtual DbSet<Testcase> Testcases { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            bool isMySql = !Database.IsSqlServer();

            // Now it is designed for SQL Server or MySQL.
            // If you add reference to MySql.Data.EntityFrameworkCore.Design,
            // you can change that line into
            //   bool isMySql = Database.IsMySQL();
            // Warning: MySQL may bring unknown errors. Not fully checked.

            modelBuilder.Entity<Submission>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Problem>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<ProblemArchive>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<AuditLog>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Detail>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Judging>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Rejudge>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<News>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<PersonRank>()
                .UseAttributes(isMySql);

            modelBuilder.Entity<Contest>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<ContestProblem>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Clarification>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Team>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<TeamAffiliation>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<TeamCategory>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<ScoreCache>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<RankCache>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Event>()
                .UseAttributes(isMySql);

            modelBuilder.Entity<Configure>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Executable>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<InternalError>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Language>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<JudgeHost>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Testcase>()
                .UseAttributes(isMySql);

            modelBuilder.Entity<User>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Role>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Student>()
                .UseAttributes(isMySql);

            if (isMySql)
            {
                modelBuilder.Entity<IdentityUserLogin<int>>()
                    .Property(e => e.LoginProvider)
                        .HasMaxLength(255);
                modelBuilder.Entity<IdentityUserLogin<int>>()
                    .Property(e => e.ProviderKey)
                        .HasMaxLength(255);
                modelBuilder.Entity<IdentityUserToken<int>>()
                    .Property(e => e.LoginProvider)
                        .HasMaxLength(255);
                modelBuilder.Entity<IdentityUserToken<int>>()
                    .Property(e => e.Name)
                        .HasMaxLength(255);
                modelBuilder.Entity<Executable>()
                    .Property(e => e.ZipFile)
                        .HasColumnType("BLOB");
            }

            modelBuilder.Entity<Role>()
                .HasData(
                    new Role { Id = -1, ConcurrencyStamp = "17337d8e-0118-4c42-9da6-e4ca600d5836", Name = "Administrator", NormalizedName = "ADMINISTRATOR", ShortName = "admin", Description = "Administrative User" },
                    new Role { Id = -2, ConcurrencyStamp = "9ec57e90-312c-4eed-ac25-a40fbcf5f33b", Name = "Blocked", NormalizedName = "BLOCKED", ShortName = "blocked", Description = "Blocked User" },
                    new Role { Id = -3, ConcurrencyStamp = "2bbf420d-6253-4ace-a825-4bf8e85cf41e", Name = "Problem", NormalizedName = "PROBLEM", ShortName = "prob", Description = "Problem Provider" },
                    new Role { Id = -4, ConcurrencyStamp = "fd0d1cf4-2ccf-4fd6-9d47-7fd62923c5d2", Name = "Judgehost", NormalizedName = "JUDGEHOST", ShortName = "judgehost", Description = "(Internal/System) Judgehost" },
                    new Role { Id = -5, ConcurrencyStamp = "81ffd1be-883c-4093-8adf-f2a4909370b7", Name = "CDS", NormalizedName = "CDS", ShortName = "cds_api", Description = "CDS API user" },
                    new Role { Id = -6, ConcurrencyStamp = "e5db5526-f7cd-41d5-aaf9-391b3ed17b3d", Name = "Student", NormalizedName = "STUDENT", ShortName = "stud", Description = "Verified Student" });

            modelBuilder.Entity<Configure>()
                .HasData(
                    new Configure { ConfigId = -1, Category = "Judging", Name = "process_limit", Value = "64", Type = "int", Description = "Maximum number of processes that the submission is allowed to start (including shell and possibly interpreters).", Public = 0 },
                    new Configure { ConfigId = -2, Category = "Judging", Name = "script_timelimit", Value = "30", Type = "int", Description = "Maximum seconds available for compile/compare scripts. This is a safeguard against malicious code and buggy scripts, so a reasonable but large amount should do.", Public = 0 },
                    new Configure { ConfigId = -3, Category = "Judging", Name = "script_memory_limit", Value = "2097152", Type = "int", Description = "Maximum memory usage (in kB) by compile/compare scripts. This is a safeguard against malicious code and buggy script, so a reasonable but large amount should do.", Public = 0 },
                    new Configure { ConfigId = -4, Category = "Judging", Name = "script_filesize_limit", Value = "540672", Type = "int", Description = "Maximum filesize (in kB) compile/compare scripts may write. Submission will fail with compiler-error when trying to write more, so this should be greater than any *intermediate or final* result written by compilers.", Public = 0 },
                    new Configure { ConfigId = -5, Category = "Judging", Name = "timelimit_overshoot", Value = "\"1s|10%\"", Type = "string", Description = "Time that submissions are kept running beyond timelimit before being killed. Specify as \"Xs\" for X seconds, \"Y%\" as percentage, or a combination of both separated by one of \"+|&\" for the sum, maximum, or minimum of both.", Public = 0 },
                    new Configure { ConfigId = -6, Category = "Judging", Name = "output_storage_limit", Value = "60000", Type = "int", Description = "Maximum size of error/system output stored in the database (in bytes); use \"-1\" to disable any limits.", Public = 0 },
                    new Configure { ConfigId = -7, Category = "Judging", Name = "diskspace_error", Value = "1048576", Type = "int", Description = "Minimum free disk space (in kB) on judgehosts.", Public = 0 },
                    new Configure { ConfigId = -8, Category = "Judging", Name = "update_judging_seconds", Value = "0", Type = "int", Description = "Post updates to a judging every X seconds. Set to 0 to update after each judging_run.", Public = 0 });

            modelBuilder.Entity<User>()
                .HasData(
                    new User { Id = -1, UserName = "judgehost", NormalizedUserName = "JUDGEHOST", Email = "acm@xylab.fun", NormalizedEmail = "ACM@XYLAB.FUN", EmailConfirmed = true, ConcurrencyStamp = "e1a1189a-38f5-487b-907b-6d0533722f02", SecurityStamp = "AAAABBBBCCCCDDDDEEEEFFFFGGGGHHHH", LockoutEnabled = false, NickName = "User for judgedaemons" });

            modelBuilder.Entity<IdentityUserRole<int>>()
                .HasData(
                    new IdentityUserRole<int> { RoleId = -4, UserId = -1 });

            modelBuilder.Query<SubmissionStatistics>();
        }


        public void UpdateScoreboard(ScoreboardState stt)
        {
            ScoreboardUpdate?.Invoke(stt);
        }
    }
}
