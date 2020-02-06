using Markdig.Syntax.Inlines;

namespace Markdig.Renderers.LaTeX.Inlines
{
    public class LineBreakInlineRenderer : LatexObjectRenderer<LineBreakInline>
    {
        public bool RenderAsHardlineBreak { get; set; }

        protected override void Write(LatexRenderer renderer, LineBreakInline obj)
        {
            if (RenderAsHardlineBreak)
                renderer.WriteLine("\\newline");
        }
    }
}