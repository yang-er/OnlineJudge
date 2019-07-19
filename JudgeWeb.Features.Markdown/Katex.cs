using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Markdig
{
    public class KatexExtension : IMarkdownExtension
    {
        public class KatexInlineParser : MathInlineParser
        {
            public KatexInlineParser() { DefaultClass = "katex-src"; }
        }

        public class KatexBlockParser : MathBlockParser
        {
            public KatexBlockParser() { DefaultClass = "katex-src"; }
        }

        public class HtmlKatexInlineRenderer : HtmlObjectRenderer<MathInline>
        {
            protected override void Write(HtmlRenderer renderer, MathInline obj)
            {
                renderer.Write("<span").WriteAttributes(obj).Write(">");
                renderer.WriteEscape(ref obj.Content);
                renderer.Write("</span>");
            }
        }

        public class HtmlKatexBlockRenderer : HtmlObjectRenderer<MathBlock>
        {
            protected override void Write(HtmlRenderer renderer, MathBlock obj)
            {
                renderer.EnsureLine();
                renderer.Write("<pre").WriteAttributes(obj).WriteLine(">");
                renderer.WriteLeafRawLines(obj, true, true);
                renderer.WriteLine("</pre>");
            }
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            // Adds the inline parser
            if (!pipeline.InlineParsers.Contains<KatexInlineParser>())
            {
                pipeline.InlineParsers.Insert(0, new KatexInlineParser());
            }

            // Adds the block parser
            if (!pipeline.BlockParsers.Contains<KatexBlockParser>())
            {
                // Insert before EmphasisInlineParser to take precedence
                pipeline.BlockParsers.Insert(0, new KatexBlockParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                if (!htmlRenderer.ObjectRenderers.Contains<HtmlKatexInlineRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Insert(0, new HtmlKatexInlineRenderer());
                }

                if (!htmlRenderer.ObjectRenderers.Contains<HtmlKatexBlockRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Insert(0, new HtmlKatexBlockRenderer());
                }
            }
        }
    }
}
