using Markdig.Syntax;

namespace Markdig.Renderers.LaTeX
{
    public class ListRenderer : LatexObjectRenderer<ListBlock>
    {
        protected override void Write(LatexRenderer renderer, ListBlock listBlock)
        {
            renderer.EnsureLine();
            renderer.WriteLine(listBlock.IsOrdered ? "\\begin{enumerate}" : "\\begin{itemize}");

            foreach (var item in listBlock)
            {
                var listItem = (ListItemBlock)item;
                var previousImplicit = renderer.ImplicitParagraph;
                renderer.ImplicitParagraph = !listBlock.IsLoose;

                renderer.EnsureLine();
                renderer.Write("\\item ");
                renderer.WriteChildren(listItem);
                renderer.EnsureLine();
                renderer.ImplicitParagraph = previousImplicit;
            }

            renderer.Write(listBlock.IsOrdered ? "\\end{enumerate}" : "\\end{itemize}");
            renderer.FinishBlock(true);
        }
    }
}