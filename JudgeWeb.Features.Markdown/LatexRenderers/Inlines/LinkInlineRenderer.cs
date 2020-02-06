using Markdig.Syntax.Inlines;

namespace Markdig.Renderers.LaTeX.Inlines
{
    public class LinkInlineRenderer : LatexObjectRenderer<LinkInline>
    {
        protected override void Write(LatexRenderer renderer, LinkInline link)
        {
            if (link.IsImage)
            {
                renderer.Write("\\includegraphics{");
                renderer.Write(link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url);
                renderer.Write("}");
            }
            else
            {
                renderer.Write("\\texttt{");
                renderer.WriteEscape(link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url);
                renderer.Write("}");
            }
        }
    }
}