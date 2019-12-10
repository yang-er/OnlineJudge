using Microsoft.Extensions.DependencyInjection;

namespace JudgeWeb.Features.OjUpdate
{
    public static class OjExtensions
    {
        public static IServiceCollection AddOjUpdateService(
            this IServiceCollection services,
            int sleepLength = 24 * 7 * 60)
        {
            OjUpdateService.SleepLength = sleepLength;
            services.AddHostedService<HdojUpdateService>();
            services.AddHostedService<CfUpdateService>();
            services.AddHostedService<VjUpdateService>();
            return services;
        }
    }
}
