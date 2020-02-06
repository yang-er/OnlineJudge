using Markdig.Syntax.Inlines;

namespace Markdig.Renderers.LaTeX.Inlines
{
    public class LiteralInlineRenderer : LatexObjectRenderer<LiteralInline>
    {
        protected override void Write(LatexRenderer renderer, LiteralInline obj)
        {
            renderer.WriteEscape(ref obj.Content);
        }
    }
}
