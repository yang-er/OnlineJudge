using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 队伍归属组织
    /// </summary>
    public class TeamAffiliation
    {
        /// <summary>
        /// 组织编号
        /// </summary>
        public int AffiliationId { get; set; }

        /// <summary>
        /// 组织正式名称
        /// </summary>
        public string FormalName { get; set; }

        /// <summary>
        /// 外部名称
        /// </summary>
        public string ExternalId { get; set; }

        /// <summary>
        /// 国家编号
        /// </summary>
        public string CountryCode { get; set; } = "CHN";
    }

    public partial class AppDbContext : IEntityTypeConfiguration<TeamAffiliation>
    {
        public void Configure(EntityTypeBuilder<TeamAffiliation> entity)
        {
            entity.HasKey(e => e.AffiliationId);

            entity.HasIndex(e => e.ExternalId);

            entity.Property(e => e.CountryCode)
                .IsUnicode(false)
                .HasMaxLength(4);
        }
    }
}
