using System.Threading.Tasks;

namespace JudgeWeb.Features.Storage
{
    public interface IFileRepository
    {
        void SetContext(string context);

        Task<string> ReadPartAsync(string backstore, string targetFile);

        Task WritePartAsync(string backstore, string targetFile, string content);

        bool ExistPart(string backstore, string targetFile);

        void RemovePart(string backstore, string targetFile);
    }
}
