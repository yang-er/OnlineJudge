using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public static class CreateZipArchiveEntryExtensions
    {
        const int LINUX644 = -2119958528;

        public static ZipArchiveEntry CreateEntryFromByteArray(this ZipArchive zip, byte[] content, string entry)
        {
            var f = zip.CreateEntry(entry);
            using (var fs = f.Open())
                fs.Write(content, 0, content.Length);
            f.ExternalAttributes = LINUX644;
            return f;
        }

        public static ZipArchiveEntry CreateEntryFromString(this ZipArchive zip, string content, string entry)
        {
            var ent = CreateEntryFromByteArray(zip, Encoding.UTF8.GetBytes(content), entry);
            ent.ExternalAttributes = LINUX644;
            return ent;
        }

        public static async Task<ZipArchiveEntry> CreateEntryFromStream(this ZipArchive zip, Stream source, string entry)
        {
            var f = zip.CreateEntry(entry);
            using (var fs = f.Open())
                await source.CopyToAsync(fs);
            f.ExternalAttributes = LINUX644;
            return f;
        }
    }
}
