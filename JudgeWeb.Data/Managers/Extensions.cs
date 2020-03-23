using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JudgeWeb.Data
{
    public static class ManagersServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultManagers(this IServiceCollection services)
        {
            return services;
        }

        public static AuthenticationBuilder AddCookie2(
            this AuthenticationBuilder builder,
            Action<CookieAuthenticationOptions> options)
        {
            builder.AddCookie(options);
            builder.Services.ConfigureApplicationCookie(options);
            return builder;
        }
    }
}
