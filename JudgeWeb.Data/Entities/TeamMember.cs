using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    public class TeamMember
    {
        /// <summary>
        /// 比赛编号
        /// </summary>
        public int ContestId { get; set; }

        /// <summary>
        /// 队伍编号
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 是否为临时账号
        /// </summary>
        public bool Temporary { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<TeamMember>
    {
        public void Configure(EntityTypeBuilder<TeamMember> entity)
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
        }
    }
}
