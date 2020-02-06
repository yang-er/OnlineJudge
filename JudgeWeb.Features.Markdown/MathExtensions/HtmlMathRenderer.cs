using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Markdig.Extensions.Math
{
    public class HtmlMathInlineRenderer : HtmlObjectRenderer<MathInline>
    {
        protected override void Write(HtmlRenderer renderer, MathInline obj)
        {
            renderer.Write("<span").WriteAttributes(obj).Write(">");
            renderer.WriteEscape(ref obj.Content);
            renderer.Write("</span>");
        }
    }

    public class HtmlMathBlockRenderer : HtmlObjectRenderer<MathBlock>
    {
        protected override void Write(HtmlRenderer renderer, MathBlock obj)
        {
            renderer.EnsureLine();
            renderer.Write("<pre").WriteAttributes(obj).WriteLine(">");
            renderer.WriteLeafRawLines(obj, true, true);
            renderer.WriteLine("</pre>");
        }
    }
}
