using Markdig.Syntax.Inlines;

namespace Markdig.Renderers.LaTeX.Inlines
{
    public class DelimiterInlineRenderer : LatexObjectRenderer<DelimiterInline>
    {
        protected override void Write(LatexRenderer renderer, DelimiterInline obj)
        {
            renderer.WriteEscape(obj.ToLiteral());
            renderer.WriteChildren(obj);
        }
    }
}