using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Extensions.FileProviders
{
    public interface IMutableFileProvider : IFileProvider
    {
        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="content">The string content to write</param>
        /// <returns>The file information.</returns>
        Task<IFileInfo> WriteStringAsync(string subpath, string content);

        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="content">The byte-array content to write</param>
        /// <returns>The file information.</returns>
        Task<IFileInfo> WriteBinaryAsync(string subpath, byte[] content);

        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="content">The stream content to write</param>
        /// <returns>The file information.</returns>
        Task<IFileInfo> WriteStreamAsync(string subpath, Stream content);

        /// <summary>
        /// Remove a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        bool RemoveFile(string subpath);

        /// <summary>
        /// Write a file at the given path.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <returns>The write stream. Caller should dispose it.</returns>
        Stream OpenWrite(string subpath);
    }
}
