using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 学生信息
    /// </summary>
    public class Student
    {
        /// <summary>
        /// 学生教学号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 学生名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 学生行政班
        /// </summary>
        public int Class { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> entity)
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .HasMaxLength(32);

            entity.Property(s => s.Id)
                .ValueGeneratedNever();
        }
    }
}
