using Markdig.Syntax;

namespace Markdig.Renderers.LaTeX
{
    public class QuoteBlockRenderer : LatexObjectRenderer<QuoteBlock>
    {
        protected override void Write(LatexRenderer renderer, QuoteBlock obj)
        {
            renderer.EnsureLine();
            renderer.WriteLine("\\begin{quote}");

            var savedImplicitParagraph = renderer.ImplicitParagraph;
            renderer.ImplicitParagraph = false;
            renderer.WriteChildren(obj);
            renderer.ImplicitParagraph = savedImplicitParagraph;

            renderer.EnsureLine();
            renderer.Write("\\end{quote}");
            renderer.FinishBlock(true);
        }
    }
}