using JudgeWeb.Features.Scoreboard;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ScoreboardServiceCollectionExtensions
    {
        public static IServiceCollection AddScoreboardService(this IServiceCollection services)
        {
            services.AddScoped<CalculateExecutor>();
            services.AddHostedService<RefreshService>();
            return services;
        }
    }
}
