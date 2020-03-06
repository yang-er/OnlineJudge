using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Storage
{
    public static class ProblemRepositoryExtensions
    {
        public static IServiceCollection AddProblemRepository(this IServiceCollection services)
        {
            services.AddSingleton<IProblemFileRepository, ProblemFileRepository>();
            services.AddSingleton<IRunFileRepository, RunFileRepository>();
            services.AddSingleton<IStaticFileRepository, StaticFileRepository>();
            return services;
        }
    }
}
