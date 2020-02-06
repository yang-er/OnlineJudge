using Markdig.Extensions.Mathematics;
using Markdig.Renderers.LaTeX;

namespace Markdig.Extensions.Math
{
    public class LatexMathInlineRenderer : LatexObjectRenderer<MathInline>
    {
        protected override void Write(LatexRenderer renderer, MathInline obj)
        {
            renderer.Write('$');
            renderer.Write(ref obj.Content);
            renderer.Write('$');
        }
    }

    public class LatexMathBlockRenderer : LatexObjectRenderer<MathBlock>
    {
        protected override void Write(LatexRenderer renderer, MathBlock obj)
        {
            renderer.EnsureLine();
            renderer.WriteLine("\\begin{equation*}");
            renderer.WriteLeafRawLines(obj, true, false);
            renderer.WriteLine("\\end{equation*}").WriteLine();
        }
    }
}
