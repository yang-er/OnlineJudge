using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace JudgeWeb.Features.Mailing
{
    public static class EmailSenderExtensions
    {
        public static OptionsBuilder<AuthMessageSenderOptions> AddEmailSender(this IServiceCollection services)
        {
            services.AddSingleton<IEmailSender, SendGridOrQQSender>();
            return services.AddOptions<AuthMessageSenderOptions>();
        }
    }
}
