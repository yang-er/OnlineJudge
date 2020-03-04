using JudgeWeb.Features;
using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.LaTeX;
using Markdig.Renderers.Normalize;

namespace Markdig.Extensions.Math
{
    public class KatexExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            // Adds the inline parser
            if (!pipeline.InlineParsers.Contains<MathInlineParser2>())
            {
                pipeline.InlineParsers.Insert(0, new MathInlineParser2
                {
                    DefaultClass = "katex-src"
                });
            }

            // Adds the block parser
            if (!pipeline.BlockParsers.Contains<MathBlockParser>())
            {
                // Insert before EmphasisInlineParser to take precedence
                pipeline.BlockParsers.Insert(0, new MathBlockParser
                {
                    DefaultClass = "katex-src"
                });
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.TryPrepend<HtmlMathInlineRenderer>();
                htmlRenderer.ObjectRenderers.TryPrepend<HtmlMathBlockRenderer>();
            }
            else if (renderer is NormalizeRenderer normalizeRenderer)
            {
                normalizeRenderer.ObjectRenderers.TryPrepend<NormalizeMathInlineRenderer>();
                normalizeRenderer.ObjectRenderers.TryPrepend<NormalizeMathBlockRenderer>();
            }
            else if (renderer is LatexRenderer latexRenderer)
            {
                latexRenderer.ObjectRenderers.TryPrepend<LatexMathInlineRenderer>();
                latexRenderer.ObjectRenderers.TryPrepend<LatexMathBlockRenderer>();
            }
        }
    }
}
