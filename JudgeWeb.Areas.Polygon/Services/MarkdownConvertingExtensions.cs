using JudgeWeb.Features;
using JudgeWeb.Features.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Services
{
    public static class MarkdownConvertingExtensions
    {
        public static Task<string> ExportWithImagesAsync(this (IMarkdownService, IStaticFileRepository) v, string content)
        {
            (IMarkdownService markdown, IStaticFileRepository files) = v;
            return markdown.SolveImagesAsync(content, async url =>
            {
                if (!url.StartsWith("/images/problem/")) return url;
                var file = files.GetFileInfo(url);
                if (!file.Exists) return url;
                var img = await file.ReadBinaryAsync();
                var imgExt = Path.GetExtension(url).TrimStart('.');
                return $"data:image/{imgExt};base64," + Convert.ToBase64String(img);
            });
        }

        public static Task<string> ImportWithImagesAsync(this (IMarkdownService, IStaticFileRepository) v, string content, string typeid)
        {
            (IMarkdownService markdown, IStaticFileRepository files) = v;
            return markdown.SolveImagesAsync(content, async url =>
            {
                if (!url.StartsWith("data:image/")) return url;
                var index = url.IndexOf(";base64,");
                if (index == -1) return url;
                string ext = url[11..index];

                // upload files
                string fileName;
                do
                {
                    var guid = Guid.NewGuid().ToString("N").Substring(0, 16);
                    fileName = $"images/problem/{typeid}.{guid}.{ext}";
                }
                while (files.GetFileInfo(fileName).Exists);

                try
                {
                    var fileIn = Convert.FromBase64String(url.Substring(index + 8));
                    await files.WriteBinaryAsync(fileName, fileIn);
                    return "/" + fileName;
                }
                catch
                {
                    return url;
                }
            });
        }
    }
}
