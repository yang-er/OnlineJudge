using Markdig.Syntax;

namespace Markdig.Renderers.LaTeX
{
    public class ParagraphRenderer : LatexObjectRenderer<ParagraphBlock>
    {
        protected override void Write(LatexRenderer renderer, ParagraphBlock obj)
        {
            if (!renderer.ImplicitParagraph)
                if (!renderer.IsFirstInContainer)
                    renderer.EnsureLine();
            renderer.WriteLeafInline(obj);
            renderer.FinishBlock(!renderer.ImplicitParagraph);
        }
    }
}