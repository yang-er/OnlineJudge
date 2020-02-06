using Markdig.Syntax.Inlines;

namespace Markdig.Renderers.LaTeX.Inlines
{
    public class AutolinkInlineRenderer : LatexObjectRenderer<AutolinkInline>
    {
        protected override void Write(LatexRenderer renderer, AutolinkInline obj)
        {
            renderer.Write("\\texttt{");
            renderer.WriteEscape(obj.Url);
            renderer.Write("}");
        }
    }
}