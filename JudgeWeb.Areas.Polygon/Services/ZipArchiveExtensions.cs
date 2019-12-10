using System.IO.Compression;
using System.Text;

namespace JudgeWeb.Areas.Polygon.Services
{
    public static class ZipArchiveExtensions
    {
        public static ZipArchiveEntry CreateEntryFromByteArray(this ZipArchive zip, byte[] content, string entry)
        {
            var f = zip.CreateEntry(entry);
            using (var fs = f.Open())
                fs.Write(content, 0, content.Length);
            return f;
        }

        public static ZipArchiveEntry CreateEntryFromString(this ZipArchive zip, string content, string entry)
        {
            return CreateEntryFromByteArray(zip, Encoding.UTF8.GetBytes(content), entry);
        }
    }
}
