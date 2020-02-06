using Markdig.Syntax;

namespace Markdig.Renderers.LaTeX
{
    public class ThematicBreakRenderer : LatexObjectRenderer<ThematicBreakBlock>
    {
        protected override void Write(LatexRenderer renderer, ThematicBreakBlock obj)
        {
            renderer.EnsureLine();
            renderer.Write("\\rule");
            renderer.FinishBlock(true);
        }
    }
}