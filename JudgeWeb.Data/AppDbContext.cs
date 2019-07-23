using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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


        public virtual DbQuery<SubmissionStatistics> SubmissionStatistics { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity2<Submission>();
            modelBuilder.Entity2<Problem>();
            modelBuilder.Entity2<AuditLog>();
            modelBuilder.Entity2<Detail>();
            modelBuilder.Entity2<Judging>();
            modelBuilder.Entity2<Rejudge>();
            modelBuilder.Entity2<News>();

            modelBuilder.Entity2<Contest>();
            modelBuilder.Entity2<ContestProblem>();
            modelBuilder.Entity2<Clarification>();
            modelBuilder.Entity2<Team>();
            modelBuilder.Entity2<TeamAffiliation>();
            modelBuilder.Entity2<TeamCategory>();
            modelBuilder.Entity2<ScoreCache>();
            modelBuilder.Entity2<RankCache>();

            modelBuilder.Entity2<Configure>();
            modelBuilder.Entity2<Executable>();
            modelBuilder.Entity2<InternalError>();
            modelBuilder.Entity2<Language>();
            modelBuilder.Entity2<JudgeHost>();
            modelBuilder.Entity2<Testcase>();

            modelBuilder.Query<SubmissionStatistics>()
                .ToView("SubmissionStatistics");
        }
    }
}
