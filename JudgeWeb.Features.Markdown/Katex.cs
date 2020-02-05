using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Normalize;

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

        public class NormalizeKatexInlineRenderer : NormalizeObjectRenderer<MathInline>
        {
            protected override void Write(NormalizeRenderer renderer, MathInline obj)
            {
                renderer.Write('$');
                renderer.Write(ref obj.Content);
                renderer.Write('$');
            }
        }

        public class NormalizeKatexBlockRenderer : NormalizeObjectRenderer<MathBlock>
        {
            protected override void Write(NormalizeRenderer renderer, MathBlock obj)
            {
                renderer.EnsureLine();
                renderer.WriteLine("$$");
                renderer.WriteLeafRawLines(obj, true);
                renderer.Write("$$");
                renderer.FinishBlock(renderer.Options.EmptyLineAfterCodeBlock);
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
            else if (renderer is NormalizeRenderer normalizeRenderer)
            {
                if (!normalizeRenderer.ObjectRenderers.Contains<NormalizeKatexInlineRenderer>())
                {
                    normalizeRenderer.ObjectRenderers.Insert(0, new NormalizeKatexInlineRenderer());
                }

                if (!normalizeRenderer.ObjectRenderers.Contains<NormalizeKatexBlockRenderer>())
                {
                    normalizeRenderer.ObjectRenderers.Insert(0, new NormalizeKatexBlockRenderer());
                }
            }
        }
    }
}
