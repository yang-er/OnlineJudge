using JudgeWeb.Features;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Services
{
    public static class MarkdownConvertingExtensions
    {
        public static Task<string> ExportWithImagesAsync(this IMarkdownService markdown, string content)
        {
            return markdown.SolveImagesAsync(content, async url =>
            {
                if (!url.StartsWith("/images/problem/")) return url;
                if (!File.Exists("wwwroot" + url)) return url;
                var img = await File.ReadAllBytesAsync("wwwroot" + url);
                var imgExt = Path.GetExtension(url).TrimStart('.');
                return $"data:image/{imgExt};base64," + Convert.ToBase64String(img);
            });
        }

        public static Task<string> ImportWithImagesAsync(this IMarkdownService markdown, string content, string typeid)
        {
            return markdown.SolveImagesAsync(content, async url =>
            {
                if (!url.StartsWith("data:image/")) return url;
                var index = url.IndexOf(";base64,");
                if (index == -1) return url;
                string ext = url.Substring(11, index - 11);

                // upload files
                string fileName;
                do
                {
                    var guid = Guid.NewGuid().ToString("N").Substring(0, 16);
                    fileName = $"/images/problem/{typeid}.{guid}.{ext}";
                }
                while (File.Exists("wwwroot" + fileName));

                try
                {
                    var fileIn = Convert.FromBase64String(url.Substring(index + 8));
                    await File.WriteAllBytesAsync("wwwroot" + fileName, fileIn);
                    return fileName;
                }
                catch
                {
                    return url;
                }
            });
        }
    }
}
