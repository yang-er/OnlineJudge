using Microsoft.Extensions.DependencyInjection;

namespace JudgeWeb.Data
{
    public static class ManagersServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultManagers(this IServiceCollection services)
        {
            services.AddScoped<SubmissionManager>();
            return services;
        }
    }
}
