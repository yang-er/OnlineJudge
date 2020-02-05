using JudgeWeb.Features;
using JudgeWeb.Features.Markdown;
using Markdig.Renderers.Html;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.IO;
using System.Threading.Tasks;

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

        public async Task<string> SolveImagesAsync(string source, Func<string, Task<string>> converter)
        {
            using (var tw = new StringWriter())
            {
                var writer = new NormalizeRenderer(tw);
                Pipeline.Setup(writer);
                var doc1 = Markdown.Parse(source, Pipeline);

                await doc1.TransverseAsync<LinkInline>(async item =>
                {
                    if (item.IsImage)
                    {
                        item.Url = await converter(item.Url);
                    }
                });

                writer.Render(doc1);
                return tw.ToString();
            }
        }
    }
}
