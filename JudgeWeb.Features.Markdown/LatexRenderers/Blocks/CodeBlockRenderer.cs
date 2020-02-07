using Markdig.Syntax;

namespace Markdig.Renderers.LaTeX
{
    public class CodeBlockRenderer : LatexObjectRenderer<CodeBlock>
    {
        protected override void Write(LatexRenderer renderer, CodeBlock obj)
        {
            renderer.EnsureLine();
            renderer.WriteLine("\\begin{lstlisting}");
            renderer.WriteLeafRawLines(obj, true, false);
            renderer.Write("\\end{lstlisting}");
            renderer.FinishBlock(true);
        }
    }
}