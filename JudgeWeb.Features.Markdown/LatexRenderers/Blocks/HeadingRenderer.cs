using Markdig.Syntax;

namespace Markdig.Renderers.LaTeX
{
    public class HeadingRenderer : LatexObjectRenderer<HeadingBlock>
    {
        protected override void Write(LatexRenderer renderer, HeadingBlock obj)
        {
            // Here we shouldn't get too much levels
            renderer.EnsureLine();
            renderer.WriteLine();
            renderer.Write("\\textbf{");
            renderer.WriteLeafInline(obj);
            renderer.Write("}");
            renderer.FinishBlock(true);
        }
    }
}