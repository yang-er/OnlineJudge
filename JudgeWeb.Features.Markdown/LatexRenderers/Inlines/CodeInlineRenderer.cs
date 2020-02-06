using Markdig.Syntax.Inlines;

namespace Markdig.Renderers.LaTeX.Inlines
{
    public class CodeInlineRenderer : LatexObjectRenderer<CodeInline>
    {
        protected override void Write(LatexRenderer renderer, CodeInline obj)
        {
            renderer.Write("\\texttt{");
            renderer.WriteEscape(obj.Content);
            renderer.Write("}");
        }
    }
}