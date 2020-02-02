using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 班级学生关系组
    /// </summary>
    public class ClassStudent
    {
        /// <summary>
        /// 学生编号
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// 行政班编号
        /// </summary>
        public int ClassId { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<ClassStudent>
    {
        public void Configure(EntityTypeBuilder<ClassStudent> entity)
        {
            entity.HasKey(e => new { e.StudentId, e.ClassId });

            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<TeachingClass>()
                .WithMany()
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
