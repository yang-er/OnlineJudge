using Markdig.Extensions.Mathematics;
using Markdig.Renderers.Normalize;

namespace Markdig.Extensions.Math
{
    public class NormalizeMathInlineRenderer : NormalizeObjectRenderer<MathInline>
    {
        protected override void Write(NormalizeRenderer renderer, MathInline obj)
        {
            renderer.Write('$');
            renderer.Write(ref obj.Content);
            renderer.Write('$');
        }
    }

    public class NormalizeMathBlockRenderer : NormalizeObjectRenderer<MathBlock>
    {
        protected override void Write(NormalizeRenderer renderer, MathBlock obj)
        {
            renderer.EnsureLine();
            renderer.WriteLine("$$");
            renderer.WriteLeafRawLines(obj, true);
            renderer.Write("$$");
            renderer.FinishBlock(renderer.Options.EmptyLineAfterCodeBlock);
        }
    }
}
