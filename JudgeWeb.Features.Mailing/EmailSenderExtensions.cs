using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Mailing
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link)
        {
            return emailSender.SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }

        public static IServiceCollection AddEmailSender(this IServiceCollection services, Action<OptionsBuilder<AuthMessageSenderOptions>> optionsBuilder)
        {
            optionsBuilder.Invoke(services.AddOptions<AuthMessageSenderOptions>());

            services.AddSingleton<IEmailSender, SendGridOrQQSender>();

            return services;
        }
    }
}
