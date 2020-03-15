using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 比赛题目
    /// </summary>
    public class ContestProblem
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 题目编号
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 短名称
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// [Ignore] 题目顺序
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 是否允许提交
        /// </summary>
        public bool AllowSubmit { get; set; }

        /// <summary>
        /// 是否允许评测
        /// </summary>
        public bool AllowJudge { get; set; }

        /// <summary>
        /// 气球颜色
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// CF模式题目分数
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// [Ignore] 题目标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// [Ignore] 题目时间限制
        /// </summary>
        public int TimeLimit { get; set; }

        /// <summary>
        /// [Ignore] 题目内存限制
        /// </summary>
        public int MemoryLimit { get; set; }

        /// <summary>
        /// [Ignore] 测试样例组数
        /// </summary>
        public int TestcaseCount { get; set; }

        /// <summary>
        /// [Ignore] 是否为交互题
        /// </summary>
        public bool Interactive { get; set; }

        /// <summary>
        /// [Ignore] 是否共享
        /// </summary>
        public bool Shared { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ContestProblem() { }

        /// <summary>
        /// 拷贝构造函数
        /// </summary>
        /// <param name="cp">拷贝源</param>
        public ContestProblem(ContestProblem cp, string tit, int time, int mem, bool ia, bool shared)
        {
            AllowJudge = cp.AllowJudge;
            AllowSubmit = cp.AllowSubmit;
            Color = cp.Color;
            ContestId = cp.ContestId;
            ProblemId = cp.ProblemId;
            Rank = cp.Rank;
            ShortName = cp.ShortName;
            Title = tit;
            TimeLimit = time;
            MemoryLimit = mem;
            Interactive = ia;
            Shared = shared;
        }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<ContestProblem>
    {
        public void Configure(EntityTypeBuilder<ContestProblem> entity)
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
        }
    }
}
