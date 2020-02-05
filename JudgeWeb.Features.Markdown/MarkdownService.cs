using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features
{
    public interface IMarkdownService
    {
        string Render(string source);

        void Render(string source, out string html, out string tree);

        Task<string> SolveImagesAsync(string source, Func<string, Task<string>> converter);
    }
}
