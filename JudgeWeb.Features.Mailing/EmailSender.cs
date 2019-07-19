using System.Threading.Tasks;

namespace JudgeWeb.Features.Mailing
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
