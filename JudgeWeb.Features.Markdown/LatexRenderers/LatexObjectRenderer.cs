using Markdig.Syntax;

namespace Markdig.Renderers.LaTeX
{
    public abstract class LatexObjectRenderer<TObject> : MarkdownObjectRenderer<LatexRenderer, TObject> where TObject : MarkdownObject
    {
    }
}
