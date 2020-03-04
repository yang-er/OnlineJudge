using JudgeWeb.Features;
using Markdig.Extensions.Math;
using Markdig.Extensions.SelfPipeline;
using Markdig.Extensions.Toc;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Markdig
{
    public class MarkdigService : IMarkdownService
    {
        public MarkdownPipeline Pipeline { get; set; }

        public MarkdigService()
        {
            Pipeline = new MarkdownPipelineBuilder()
                .Use<KatexExtension>()
                .Use<SampCodeBlockExtension>()
                .Use<HeadingIdExtension>()
                .UseSoftlineBreakAsHardlineBreak()
                .UseNoFollowLinks()
                .UsePipeTables()
                .UseBootstrap()
                .Build();
        }

        public MarkdownDocument Parse(string source)
        {
            var selfPipeline = Pipeline.Extensions.Find<SelfPipelineExtension>();
            if (selfPipeline != null)
                Pipeline = selfPipeline.CreatePipelineFromInput(source);
            return Markdown.Parse(source, Pipeline);
        }

        public string RenderAsHtml(MarkdownDocument doc)
        {
            using var textWriter = new StringWriter();
            var renderer = new HtmlRenderer(textWriter);
            Pipeline.Setup(renderer);
            renderer.Render(doc);
            renderer.Writer.Flush();
            return textWriter.ToString();
        }

        public async Task<string> SolveImagesAsync(string source, Func<string, Task<string>> converter)
        {
            using (var tw = new StringWriter())
            {
                var writer = new NormalizeRenderer(tw);
                Pipeline.Setup(writer);
                var doc1 = Parse(source);

                await doc1.TransverseAsync<LinkInline>(async item =>
                {
                    if (item.IsImage)
                    {
                        item.Url = await converter(item.Url);
                    }
                });

                writer.Render(doc1);
                writer.Writer.Flush();
                return tw.ToString();
            }
        }

        public string TocAsHtml(MarkdownDocument doc)
        {
            var node = new HeadingNode();
            foreach (var item in doc.OfType<HeadingBlock>())
            {
                node.Insert(new HeadingNode
                {
                    Id = item.GetAttributes().Id,
                    Level = item.Level,
                    Title = item.Inline?.FirstChild.ToString()
                });
            }

            return node.ToString();
        }
    }
}
