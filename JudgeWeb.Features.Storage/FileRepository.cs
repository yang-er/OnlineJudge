using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace JudgeWeb.Features.Storage
{
    public class ProblemFileRepository : PhysicalMutableFileProvider, IProblemFileRepository
    {
        public ProblemFileRepository(IHostEnvironment root)
            : base(Path.Combine(root.ContentRootPath, "Problems"))
        {
        }
    }

    public class RunFileRepository : PhysicalMutableFileProvider, IRunFileRepository
    {
        public RunFileRepository(IHostEnvironment root)
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
}