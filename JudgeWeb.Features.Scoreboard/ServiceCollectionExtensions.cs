using JudgeWeb.Features.Scoreboard;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ScoreboardServiceCollectionExtensions
    {
        [Obsolete("This feature is not supported any more", true)]
        public static IServiceCollection AddScoreboardService(this IServiceCollection services)
        {
            services.AddScoped<CalculateExecutor>();
            services.AddHostedService<RefreshService>();
            return services;
        }
    }
}
