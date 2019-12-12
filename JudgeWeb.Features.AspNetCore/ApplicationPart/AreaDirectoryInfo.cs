using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    internal class AreaDirectoryInfo : IFileInfo
    {
        private Physical.PhysicalDirectoryInfo Physical { get; }

        public bool Exists => Physical.Exists;

        public long Length => Physical.Length;

        public string PhysicalPath => Physical.PhysicalPath;

        public string Name { get; }

        public DateTimeOffset LastModified => Physical.LastModified;

        public bool IsDirectory => Physical.IsDirectory;

        public Stream CreateReadStream() => Physical.CreateReadStream();

        public AreaDirectoryInfo(Physical.PhysicalDirectoryInfo physical, string name)
        {
            Physical = physical;
            Name = name;
        }
    }
}
