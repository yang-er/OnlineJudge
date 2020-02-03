using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 教学班信息
    /// </summary>
    public class TeachingClass
    {
        /// <summary>
        /// 教学班编号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 教学班名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 教学班人数
        /// </summary>
        public int Count { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<TeachingClass>
    {
        public void Configure(EntityTypeBuilder<TeachingClass> entity)
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .HasMaxLength(64);

            entity.Ignore(s => s.Count);
        }
    }
}
