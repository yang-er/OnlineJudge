namespace JudgeWeb.Features.Mailing
{
    /// <summary>
    /// 邮件发送密钥的选项
    /// </summary>
    public class AuthMessageSenderOptions
    {
        public string SendGridUser { get; set; }

        public string SendGridKey { get; set; }

        public string QQUser { get; set; }

        public string QQKey { get; set; }
    }
}
