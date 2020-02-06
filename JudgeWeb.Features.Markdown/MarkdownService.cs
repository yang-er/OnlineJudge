using Markdig;
using Markdig.Syntax;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features
{
    public interface IMarkdownService
    {
        MarkdownPipeline Pipeline { get; }

        MarkdownDocument Parse(string source);

        string RenderAsHtml(MarkdownDocument doc);

        string TocAsHtml(MarkdownDocument doc);

        Task<string> SolveImagesAsync(string source, Func<string, Task<string>> converter);
    }
}
