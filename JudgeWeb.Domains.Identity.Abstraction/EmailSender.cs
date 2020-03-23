using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Mailing
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);

        public Task SendEmailConfirmationAsync(string email, string link)
        {
            return SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }
    }
}
