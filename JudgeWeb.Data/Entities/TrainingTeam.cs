using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 训练队伍
    /// </summary>
    public class TrainingTeam
    {
        /// <summary>
        /// 队伍编号
        /// </summary>
        public int TrainingTeamId { get; set; }

        /// <summary>
        /// 队伍名称
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 学校
        /// </summary>
        public int AffiliationId { get; set; }

        /// <summary>
        /// 队伍创建者
        /// </summary>
        public int UserId { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<TrainingTeam>
    {
        public void Configure(EntityTypeBuilder<TrainingTeam> entity)
        {
            entity.HasKey(e => e.TrainingTeamId);

            entity.Property(e => e.TeamName)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasOne<TeamAffiliation>()
                .WithMany()
                .HasForeignKey(e => e.AffiliationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
