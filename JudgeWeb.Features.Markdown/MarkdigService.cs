using JudgeWeb.Features;
using JudgeWeb.Features.Markdown;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.IO;

namespace Markdig
{
    public class MarkdigService : IMarkdownService
    {
        private MarkdownPipeline Pipeline { get; }

        public MarkdigService(MarkdownPipeline mdpl)
        {
            Pipeline = mdpl;
        }

        public string Render(string source)
        {
            return Markdown.ToHtml(source, Pipeline);
        }

        public void Render(string source, out string html, out string tree)
        {
            using (var sw = new StringWriter())
            {
                var doc = Markdown.ToHtml(source, sw, Pipeline);
                html = sw.ToString();
                var node = new HeadingNode();

                foreach (var item in doc)
                {
                    if (item is HeadingBlock hb)
                    {
                        node.Insert(new HeadingNode
                        {
                            Id = hb.GetAttributes().Id,
                            Level = hb.Level,
                            Title = hb.Inline?.FirstChild.ToString()
                        });
                    }
                }

                tree = node.ToString();
            }
        }
    }
}
