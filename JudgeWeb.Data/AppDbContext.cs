using EntityFrameworkCore.Cacheable;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace JudgeWeb.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }


        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<Submission> Submissions { get; set; }
        public virtual DbSet<Problem> Problems { get; set; }
        public virtual DbSet<Detail> Details { get; set; }
        public virtual DbSet<Judging> Judgings { get; set; }
        public virtual DbSet<Rejudge> Rejudges { get; set; }
        public virtual DbSet<News> News { get; set; }


        public virtual DbSet<Contest> Contests { get; set; }
        public virtual DbSet<ContestProblem> ContestProblem { get; set; }
        public virtual DbSet<Clarification> Clarifications { get; set; }
        public virtual DbSet<Team> Teams { get; set; }
        public virtual DbSet<TeamAffiliation> TeamAffiliations { get; set; }
        public virtual DbSet<TeamCategory> TeamCategories { get; set; }
        public virtual DbSet<RankCache> RankCache { get; set; }
        public virtual DbSet<ScoreCache> ScoreCache { get; set; }


        public virtual DbSet<Configure> Configures { get; set; }
        public virtual DbSet<Executable> Executable { get; set; }
        public virtual DbSet<InternalError> InternalErrors { get; set; }
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<JudgeHost> JudgeHosts { get; set; }
        public virtual DbSet<Testcase> Testcases { get; set; }


        public IEnumerable<SubmissionStatistics> SubmissionStatistics =>
            Query<SubmissionStatistics>()
                .FromSql(Data.SubmissionStatistics.QueryString)
                .AsNoTracking()
                .Cacheable(TimeSpan.FromMinutes(10))
                .ToList();


        public IQueryable<ContestTestcase> ContestTestcase(int _cid) =>
            Query<ContestTestcase>()
                .FromSql(Data.ContestTestcase.QueryString, new SqlParameter("__cid", _cid))
                .AsNoTracking();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            bool isMySql = !Database.IsSqlServer();

            // Now it is designed for SQL Server or MySQL.
            // If you add reference to MySql.Data.EntityFrameworkCore.Design,
            // you can change that line into
            //   bool isMySql = Database.IsMySQL();

            modelBuilder.Entity<Submission>()
                .UseAttributes(isMySql);
            modelBuilder.Entity<Problem>()
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

            modelBuilder.Query<SubmissionStatistics>();
            modelBuilder.Query<ContestTestcase>();
        }
    }
}
