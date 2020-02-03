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
        /// 对应本站用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 学生邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool? IsVerified { get; set; }
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

            entity.Ignore(s => s.UserName);

            entity.Ignore(s => s.Email);

            entity.Ignore(s => s.UserId);

            entity.Ignore(s => s.IsVerified);
        }
    }
}
