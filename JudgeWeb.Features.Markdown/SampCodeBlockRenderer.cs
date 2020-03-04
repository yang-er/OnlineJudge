using Markdig.Helpers;
using Markdig.Syntax;
using System.Text.Encodings.Web;

namespace Markdig.Renderers.Html
{
    public class SampCodeBlockRenderer : CodeBlockRenderer
    {
        private readonly HtmlEncoder HtmlEncoder = HtmlEncoder.Default;

        private void WriteStartWith(HtmlRenderer renderer, ref StringLineGroup slices, char ch)
        {
            HtmlEncoder.Encode(renderer.Writer, "\n");
            for (int i = 0; i < slices.Count; i++)
            {
                ref StringSlice slice = ref slices.Lines[i].Slice;
                if (slice.CurrentChar == ch)
                    HtmlEncoder.Encode(renderer.Writer, slice.Text, slice.Start + 2, slice.Length - 2);
                HtmlEncoder.Encode(renderer.Writer, "\n");
            }
        }

        private void WriteSample(HtmlRenderer renderer, FencedCodeBlock obj)
        {
            renderer.EnsureLine();
            renderer.Write("<div class=\"samp row ml-0 mr-0 mb-3\">");
            renderer.Write("<div class=\"input col-6 pl-0 pr-0\"><div class=\"title\">Input</div><pre>");
            WriteStartWith(renderer, ref obj.Lines, '>');
            renderer.Write("</pre></div>");
            renderer.Write("<div class=\"output col-6 pl-0 pr-0 mb-0\" style=\"top:0;left:-1px\"><div class=\"title\">Output</div><pre>");
            WriteStartWith(renderer, ref obj.Lines, '<');
            renderer.Write("</pre></div>");
            renderer.Write("</div>");
            renderer.EnsureLine();
        }

        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            if (obj is FencedCodeBlock fcb && fcb.Info == "samp")
                WriteSample(renderer, fcb);
            else
                base.Write(renderer, obj);
        }
    }

    public class SampCodeBlockExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.Replace<CodeBlockRenderer>(new SampCodeBlockRenderer());
            }
        }
    }
}
