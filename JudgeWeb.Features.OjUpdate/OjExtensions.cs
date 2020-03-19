using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JudgeWeb.Features.OjUpdate
{
    public static class OjExtensions
    {
        public static IServiceCollection AddOjUpdateService<TContext>(
            this IServiceCollection services,
            int sleepLength = 24 * 7 * 60)
            where TContext : DbContext
        {
            OjUpdateService.SleepLength = sleepLength;
            services.AddHostedService<HdojUpdateService>();
            services.AddHostedService<CfUpdateService>();
            services.AddHostedService<VjUpdateService>();
            services.AddScoped<IDbContextHolder, DbContextHolder<TContext>>();
            return services;
        }
    }
}
