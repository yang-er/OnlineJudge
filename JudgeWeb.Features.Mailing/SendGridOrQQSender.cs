using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Mailing
{
    public class SendGridOrQQSender : IEmailSender
    {
        private ILogger<SendGridOrQQSender> Logger { get; }
        public AuthMessageSenderOptions ApiKey { get; set; }

        public SendGridOrQQSender(ILogger<SendGridOrQQSender> logger, IOptions<AuthMessageSenderOptions> options)
        {
            Logger = logger;
            ApiKey = options.Value;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            if (email?.EndsWith("@qq.com") ?? false)
                return SendQQAsync(email, subject, message);
            else
                return SendGridAsync(email, subject, message);
        }

        private Task SendGridAsync(string email, string subject, string message)
        {
            var client = new SendGridClient(ApiKey.SendGridKey);

            var msg = new SendGridMessage
            {
                From = new EmailAddress("acm@xylab.fun", "小羊实验室"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message,
            };

            msg.AddTo(new EmailAddress(email));
            msg.SetClickTracking(false, false);

            Logger.LogInformation("Using SendGrid. An email will be sent to {Email}", email);
            return client.SendEmailAsync(msg);
        }

        private Task SendQQAsync(string email, string subject, string message)
        {
            var msg = new MailMessage();
            msg.To.Add(email);
            msg.From = new MailAddress("webmaster@90yang.com", "小羊实验室");

            msg.Subject = subject;
            msg.SubjectEncoding = Encoding.UTF8;

            msg.IsBodyHtml = true;
            msg.Body = message;
            msg.BodyEncoding = Encoding.UTF8;

            var client = new SmtpClient
            {
                Host = "smtp.qq.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(ApiKey.QQUser, ApiKey.QQKey)
            };

            Logger.LogInformation("Fallback to QQmail. An email will be sent to {Email}", email);
            return client.SendMailAsync(msg);
        }
    }
}
