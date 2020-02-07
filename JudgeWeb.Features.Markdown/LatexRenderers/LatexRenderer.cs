using Markdig.Helpers;
using Markdig.Renderers.LaTeX.Inlines;
using Markdig.Syntax;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Markdig.Renderers.LaTeX
{
    public class LatexRenderer : TextRendererBase<LatexRenderer>
    {
        public bool ImplicitParagraph { get; set; }

        public LatexRenderer(TextWriter writer) : base(writer)
        {
            // Default block renderers
            ObjectRenderers.Add(new CodeBlockRenderer());
            ObjectRenderers.Add(new ListRenderer());
            ObjectRenderers.Add(new HeadingRenderer());
            ObjectRenderers.Add(new HtmlBlockRenderer());
            ObjectRenderers.Add(new ParagraphRenderer());
            ObjectRenderers.Add(new QuoteBlockRenderer());
            ObjectRenderers.Add(new ThematicBreakRenderer());

            // Default inline renderers
            ObjectRenderers.Add(new AutolinkInlineRenderer());
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new DelimiterInlineRenderer());
            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new LineBreakInlineRenderer());
            ObjectRenderers.Add(new HtmlInlineRenderer());
            ObjectRenderers.Add(new HtmlEntityInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer());
            ObjectRenderers.Add(new LiteralInlineRenderer());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LatexRenderer WriteEscape(string content)
        {
            if (string.IsNullOrEmpty(content))
                return this;

            WriteEscape(content, 0, content.Length);
            return this;
        }

        public void FinishBlock(bool emptyLine)
        {
            if (!IsLastInContainer)
            {
                WriteLine();
                if (emptyLine) WriteLine();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LatexRenderer WriteEscape(ref StringSlice slice)
        {
            if (slice.Start > slice.End)
            {
                return this;
            }

            return WriteEscape(slice.Text, slice.Start, slice.Length);
        }

        public LatexRenderer WriteEscape(string content, int offset, int length)
        {
            if (string.IsNullOrEmpty(content) || length == 0)
                return this;

            var end = offset + length;
            int previousOffset = offset;

            void FlushAs(string str)
            {
                Write(content, previousOffset, offset - previousOffset);
                Write(str);
                previousOffset = offset + 1;
            }

            for (; offset < end; offset++)
            {
                switch (content[offset])
                {
                    case '_': FlushAs("\\_"); break;
                    case '#': FlushAs("\\#"); break;
                    case '\\': FlushAs("\\backslash"); break;
                    case '{': FlushAs("\\lbrace"); break;
                    case '}': FlushAs("\\rbrace"); break;
                    case '~': FlushAs("\\~{}"); break;
                    case '^': FlushAs("\\^{}"); break;
                    case '&': FlushAs("\\&"); break;
                }
            }

            Write(content, previousOffset, end - previousOffset);
            return this;
        }

        public LatexRenderer WriteLeafRawLines(LeafBlock leafBlock, bool writeEndOfLines, bool escape)
        {
            if (leafBlock == null) throw new ArgumentNullException(nameof(leafBlock));

            if (leafBlock.Lines.Lines != null)
            {
                var lines = leafBlock.Lines;
                var slices = lines.Lines;

                for (int i = 0; i < lines.Count; i++)
                {
                    if (!writeEndOfLines && i > 0) WriteLine();
                    if (escape) WriteEscape(ref slices[i].Slice);
                    else Write(ref slices[i].Slice);
                    if (writeEndOfLines) WriteLine();
                }
            }

            return this;
        }
    }
}
