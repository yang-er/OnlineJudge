using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Storage
{
    public class LocalStorageRepository : IFileRepository
    {
        private string _ctx;

        private void EnsureDirectoryExists(string backstore)
        {
            if (!Directory.Exists(_ctx))
                Directory.CreateDirectory(_ctx);
            if (!Directory.Exists($"{_ctx}/{backstore}"))
                Directory.CreateDirectory($"{_ctx}/{backstore}");
        }

        public Task<string> ReadPartAsync(string backstore, string targetFile)
        {
            EnsureDirectoryExists(backstore);
            var forFile = $"{_ctx}/{backstore}/{targetFile}";
            if (File.Exists(forFile))
                return File.ReadAllTextAsync(forFile);
            return Task.FromResult<string>(null);
        }

        public Task WritePartAsync(string backstore, string targetFile, string content)
        {
            EnsureDirectoryExists(backstore);
            var forFile = $"{_ctx}/{backstore}/{targetFile}";
            return File.WriteAllTextAsync(forFile, content);
        }

        public bool ExistPart(string backstore, string targetFile)
        {
            EnsureDirectoryExists(backstore);
            return File.Exists($"{_ctx}/{backstore}/{targetFile}");
        }

        public void RemovePart(string backstore, string targetFile)
        {
            if (ExistPart(backstore, targetFile))
            {
                File.Delete($"{_ctx}/{backstore}/{targetFile}");
            }
        }

        public void SetContext(string context)
        {
            _ctx = context;
        }

        public Task WriteBinaryAsync(string backstore, string targetFile, byte[] content)
        {
            EnsureDirectoryExists(backstore);
            var forFile = $"{_ctx}/{backstore}/{targetFile}";
            return File.WriteAllBytesAsync(forFile, content);
        }
    }
}
