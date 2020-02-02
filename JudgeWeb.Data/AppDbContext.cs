using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JudgeWeb.Data
{
    public partial class AppDbContext : IdentityDbContext<User, Role, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Student> Students { get; set; }
        public virtual DbSet<TeachingClass> Classes { get; set; }
        public virtual DbSet<ClassStudent> ClassStudent { get; set; }
        public virtual DbSet<PersonRank> PersonRanks { get; set; }
        public virtual DbSet<News> News { get; set; }
        public virtual DbSet<Auditlog> Auditlogs { get; set; }

        public virtual DbSet<Configure> Configures { get; set; }
        public virtual DbSet<Executable> Executable { get; set; }
        public virtual DbSet<InternalError> InternalErrors { get; set; }
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<JudgeHost> JudgeHosts { get; set; }

        public virtual DbSet<Submission> Submissions { get; set; }
        public virtual DbSet<Problem> Problems { get; set; }
        public virtual DbSet<ProblemArchive> Archives { get; set; }
        public virtual DbSet<Testcase> Testcases { get; set; }
        public virtual DbSet<Rejudge> Rejudges { get; set; }
        public virtual DbSet<Judging> Judgings { get; set; }
        public virtual DbSet<Detail> Details { get; set; }

        public virtual DbSet<Contest> Contests { get; set; }
        public virtual DbSet<ContestProblem> ContestProblem { get; set; }
        public virtual DbSet<Team> Teams { get; set; }
        public virtual DbSet<TeamAffiliation> TeamAffiliations { get; set; }
        public virtual DbSet<TeamCategory> TeamCategories { get; set; }
        public virtual DbSet<Clarification> Clarifications { get; set; }
        public virtual DbSet<RankCache> RankCache { get; set; }
        public virtual DbSet<ScoreCache> ScoreCache { get; set; }
        public virtual DbSet<Event> Events { get; set; }
        public virtual DbSet<Balloon> Balloon { get; set; }
        public virtual DbSet<Printing> Printing { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users and permissions
            modelBuilder.ApplyConfiguration<User>(this);
            modelBuilder.ApplyConfiguration<Role>(this);
            modelBuilder.ApplyConfiguration<Student>(this);
            modelBuilder.ApplyConfiguration<TeachingClass>(this);
            modelBuilder.ApplyConfiguration<ClassStudent>(this);
            modelBuilder.ApplyConfiguration<PersonRank>(this);
            modelBuilder.ApplyConfiguration<News>(this);
            modelBuilder.ApplyConfiguration<Auditlog>(this);
            modelBuilder.Entity<IdentityUserRole<int>>()
                .HasData(new IdentityUserRole<int> { RoleId = -4, UserId = -1 });

            // Judging basis
            modelBuilder.ApplyConfiguration<Configure>(this);
            modelBuilder.ApplyConfiguration<Executable>(this);
            modelBuilder.ApplyConfiguration<InternalError>(this);
            modelBuilder.ApplyConfiguration<Language>(this);
            modelBuilder.ApplyConfiguration<JudgeHost>(this);

            // Submissions and judgings
            modelBuilder.ApplyConfiguration<Submission>(this);
            modelBuilder.ApplyConfiguration<Problem>(this);
            modelBuilder.ApplyConfiguration<ProblemArchive>(this);
            modelBuilder.ApplyConfiguration<Testcase>(this);
            modelBuilder.ApplyConfiguration<Rejudge>(this);
            modelBuilder.ApplyConfiguration<Judging>(this);
            modelBuilder.ApplyConfiguration<Detail>(this);

            // Contests and its informations
            modelBuilder.ApplyConfiguration<Contest>(this);
            modelBuilder.ApplyConfiguration<ContestProblem>(this);
            modelBuilder.ApplyConfiguration<Team>(this);
            modelBuilder.ApplyConfiguration<TeamAffiliation>(this);
            modelBuilder.ApplyConfiguration<TeamCategory>(this);
            modelBuilder.ApplyConfiguration<Clarification>(this);
            modelBuilder.ApplyConfiguration<RankCache>(this);
            modelBuilder.ApplyConfiguration<ScoreCache>(this);
            modelBuilder.ApplyConfiguration<Event>(this);
            modelBuilder.ApplyConfiguration<Balloon>(this);
            modelBuilder.ApplyConfiguration<Printing>(this);

            modelBuilder.Query<SubmissionStatistics>();
        }
    }
}
