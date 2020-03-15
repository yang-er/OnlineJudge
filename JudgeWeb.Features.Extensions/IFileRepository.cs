using Microsoft.Extensions.FileProviders;

namespace JudgeWeb.Features.Storage
{
    public interface IRunFileRepository : IMutableFileProvider { }

    public interface IProblemFileRepository : IMutableFileProvider { }

    public interface IStaticFileRepository : IMutableFileProvider { }
}
