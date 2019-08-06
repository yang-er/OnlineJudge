using Microsoft.Extensions.DependencyInjection;

namespace JudgeWeb.Data
{
    public static class ManagersServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultManagers(this IServiceCollection services)
        {
            services.AddScoped<SubmissionManager>();
            services.AddScoped<JudgingManager>();
            services.AddScoped<TestcaseManager>();
            services.AddScoped<LanguageManager>();
            return services;
        }
    }
}
