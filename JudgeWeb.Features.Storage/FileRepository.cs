using System.Threading.Tasks;

namespace JudgeWeb.Features.Storage
{
    public interface IFileRepository
    {
        void SetContext(string context);

        Task<string> ReadPartAsync(string backstore, string targetFile);

        Task<byte[]> ReadBinaryAsync(string backstore, string targetFile);

        Task WritePartAsync(string backstore, string targetFile, string content);

        Task WriteBinaryAsync(string backstore, string targetFile, byte[] content);

        bool ExistPart(string backstore, string targetFile);

        void RemovePart(string backstore, string targetFile);
    }
}
