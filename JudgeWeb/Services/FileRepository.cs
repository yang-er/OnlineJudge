using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace JudgeWeb.Features.Storage
{
    public class ProblemFileRepository : PhysicalMutableFileProvider, IProblemFileRepository
    {
        public ProblemFileRepository(IWebHostEnvironment root)
            : base(Path.Combine(root.ContentRootPath, "Problems"))
        {
        }
    }

    public class RunFileRepository : PhysicalMutableFileProvider, IRunFileRepository
    {
        public RunFileRepository(IWebHostEnvironment root)
            : base(Path.Combine(root.ContentRootPath, "Runs"))
        {
        }
    }

    public class StaticFileRepository : PhysicalMutableFileProvider, IStaticFileRepository
    {
        public StaticFileRepository(IWebHostEnvironment root)
            : base(root.WebRootPath)
        {
        }
    }

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