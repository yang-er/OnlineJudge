using Markdig.Syntax.Inlines;
using System.Diagnostics;

namespace Markdig.Renderers.LaTeX.Inlines
{
    public class EmphasisInlineRenderer : LatexObjectRenderer<EmphasisInline>
    {
        public delegate string GetTagDelegate(EmphasisInline obj);

        public EmphasisInlineRenderer()
        {
            GetTag = GetDefaultTag;
        }

        public GetTagDelegate GetTag { get; set; }

        protected override void Write(LatexRenderer renderer, EmphasisInline obj)
        {
            string tag = GetTag(obj);
            renderer.Write(tag).Write('{');
            renderer.WriteChildren(obj);
            renderer.Write('}');
        }

        public string GetDefaultTag(EmphasisInline obj)
        {
            if (obj.DelimiterChar == '*' || obj.DelimiterChar == '_')
            {
                Debug.Assert(obj.DelimiterCount <= 2);
                return obj.DelimiterCount == 2 ? "\\textbf" : "\\textit";
            }
            return "";
        }
    }
}