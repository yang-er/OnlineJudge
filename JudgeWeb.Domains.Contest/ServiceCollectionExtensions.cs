using Microsoft.Extensions.DependencyInjection;

namespace JudgeWeb.Domains.Contests
{
    public static class ServiceCollectionExtensions
    {
        public static void AddContestDomain(this IServiceCollection services)
        {
            services.TryAddFrom(typeof(ServiceCollectionExtensions).Assembly);
        }
    }
}
