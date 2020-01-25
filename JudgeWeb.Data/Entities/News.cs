using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace JudgeWeb.Data
{
    /// <summary>
    /// 新闻
    /// </summary>
    public class News
    {
        /// <summary>
        /// 新闻编号
        /// </summary>
        public int NewsId { get; set; }

        /// <summary>
        /// 是否展示给公众
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// 新闻标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 上次更新时间
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; }

        /// <summary>
        /// Markdown源码
        /// </summary>
        public byte[] Source { get; set; }

        /// <summary>
        /// Html正文
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// Html目录树结构
        /// </summary>
        public byte[] Tree { get; set; }
    }

    public partial class AppDbContext : IEntityTypeConfiguration<News>
    {
        public void Configure(EntityTypeBuilder<News> entity)
        {
            entity.HasKey(n => n.NewsId);
        }
    }
}
