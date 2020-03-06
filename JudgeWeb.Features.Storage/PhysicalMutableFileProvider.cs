using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Extensions.FileProviders
{
    public class PhysicalMutableFileProvider : PhysicalFileProvider, IMutableFileProvider
    {
        /// <summary>
        /// Initializes a new instance of a PhysicalMutableFileProvider at the given root directory.
        /// </summary>
        /// <param name="root">The root directory. This should be an absolute path.</param>
        public PhysicalMutableFileProvider(string root) : base(root)
        {
            /*
            var methodInfo = typeof(PhysicalFileProvider).GetMethod(
                name: nameof(GetFullPath),
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic);
            _getFullPath = (Func<string, string>)
                methodInfo.CreateDelegate(typeof(Func<string, string>), this);
            */
        }


        /// <summary>
        /// Remove a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        public bool RemoveFile(string subpath)
        {
            var fileInfo = GetFileInfo(subpath);
            if (fileInfo is NotFoundFileInfo || !fileInfo.Exists)
                return false;

            File.Delete(fileInfo.PhysicalPath);
            return true;
        }


        /// <summary>
        /// Ensure the directory exists.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        private void EnsureDirectoryExists(string subpath)
        {
            var path = Path.GetDirectoryName(subpath);
            Directory.CreateDirectory(path);
        }


        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="content">The byte-array content to write</param>
        /// <returns>The file information.</returns>
        public async Task<IFileInfo> WriteBinaryAsync(string subpath, byte[] content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            var fileInfo = GetFileInfo(subpath);
            if (fileInfo is NotFoundFileInfo || fileInfo.IsDirectory)
                throw new InvalidOperationException();
            EnsureDirectoryExists(fileInfo.PhysicalPath);

            var fileInfo2 = new FileInfo(fileInfo.PhysicalPath);
            using var fs = fileInfo2.OpenWrite();
            await fs.WriteAsync(content, 0, content.Length);
            return fileInfo;
        }


        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="content">The string content to write</param>
        /// <returns>The file information.</returns>
        public async Task<IFileInfo> WriteStringAsync(string subpath, string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            var fileInfo = GetFileInfo(subpath);
            if (fileInfo is NotFoundFileInfo || fileInfo.IsDirectory)
                throw new InvalidOperationException();
            EnsureDirectoryExists(fileInfo.PhysicalPath);

            var fileInfo2 = new FileInfo(fileInfo.PhysicalPath);
            using var fs = fileInfo2.OpenWrite();
            using var fw = new StreamWriter(fs);
            await fw.WriteAsync(content);
            return fileInfo;
        }


        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="content">The stream content to write</param>
        /// <returns>The file information.</returns>
        public async Task<IFileInfo> WriteStreamAsync(string subpath, Stream content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            var fileInfo = GetFileInfo(subpath);
            if (fileInfo is NotFoundFileInfo || fileInfo.IsDirectory)
                throw new InvalidOperationException();
            EnsureDirectoryExists(fileInfo.PhysicalPath);

            var fileInfo2 = new FileInfo(fileInfo.PhysicalPath);
            using var fs = fileInfo2.OpenWrite();
            await content.CopyToAsync(fs);
            return fileInfo;
        }


        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <returns>The write stream. Caller should dispose it.</returns>
        public Stream OpenWrite(string subpath)
        {
            var fileInfo = GetFileInfo(subpath);
            if (fileInfo is NotFoundFileInfo || fileInfo.IsDirectory)
                throw new InvalidOperationException();
            EnsureDirectoryExists(fileInfo.PhysicalPath);

            var fileInfo2 = new FileInfo(fileInfo.PhysicalPath);
            return fileInfo2.OpenWrite();
        }
    }
}
