using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    public class TrainingTeamUser
    {
        /// <summary>
        /// 队伍编号
        /// </summary>
        public int TrainingTeamId { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 接受邀请
        /// </summary>
        public bool? Accepted { get; set; }

        /// <summary>
        /// [Ignore] 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// [Ignore] 用户邮箱
        /// </summary>
        public string UserEmail { get; set; }

        public TrainingTeamUser() { }

        public TrainingTeamUser(TrainingTeamUser t, string un, string ue)
        {
            TrainingTeamId = t.TrainingTeamId;
            UserId = t.UserId;
            Accepted = t.Accepted;
            UserName = un;
            UserEmail = ue;
        }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<TrainingTeamUser>
    {
        public void Configure(EntityTypeBuilder<TrainingTeamUser> entity)
        {
            entity.HasKey(e => new { e.TrainingTeamId, e.UserId });

            entity.HasOne<TrainingTeam>()
                .WithMany()
                .HasForeignKey(e => e.TrainingTeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.UserName);

            entity.Ignore(e => e.UserEmail);
        }
    }
}
