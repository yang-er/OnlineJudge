using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Domains.Problems;
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
        public virtual DbSet<TrainingTeam> TrainingTeams { get; set; }
        public virtual DbSet<TrainingTeamUser> TrainingTeamUsers { get; set; }

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
        public virtual DbSet<TeamMember> TeamMembers { get; set; }
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
            modelBuilder.Apply<IdentityEntityTypeConfiguration>();
            modelBuilder.Apply<ProblemEntityTypeConfiguration>();
            modelBuilder.Apply<JudgementEntityTypeConfiguration>();
            modelBuilder.Apply<ContestEntityTypeConfiguration>();
        }
    }
}
