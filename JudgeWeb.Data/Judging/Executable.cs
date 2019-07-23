namespace JudgeWeb.Data
{
    /// <summary>
    /// 可执行文件
    /// </summary>
    public class Executable
    {
        /// <summary>
        /// 程序编号
        /// </summary>
        [Key]
        [Property(IsRequired = true, IsUnicode = false, MaxLength = 64)]
        public string ExecId { get; set; }

        /// <summary>
        /// 压缩包MD5
        /// </summary>
        [Property(IsRequired = true, MaxLength = 32, IsUnicode = false)]
        public string Md5sum { get; set; }

        /// <summary>
        /// 压缩包
        /// </summary>
        [Property(IsRequired = true)]
        public byte[] ZipFile { get; set; }

        /// <summary>
        /// 压缩包大小
        /// </summary>
        public int ZipSize { get; set; }

        /// <summary>
        /// 可执行文件描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 可执行文件类型
        /// </summary>
        public string Type { get; set; }
    }
}
