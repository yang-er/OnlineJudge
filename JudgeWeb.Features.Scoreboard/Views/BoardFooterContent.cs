using Microsoft.AspNetCore.Html;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;

namespace JudgeWeb.Features.Scoreboard
{
    public class BoardFooterContent : IHtmlContent
    {
        private HashSet<(string, string)> _cats { get; }

        public BoardFooterContent(HashSet<(string, string)> cats) => _cats = cats;

        public void WriteTo(TextWriter writer, HtmlEncoder encoder) => WriteTo(_cats, writer, encoder);

        public static void WriteTo(HashSet<(string, string)> cats, TextWriter writer, HtmlEncoder encoder)
        {
            writer.WriteLine("<p><br /><br /></p>");

            if (cats.Count > 1)
            {
                writer.WriteLine("<table id=\"categ_legend\" class=\"scoreboard scorelegend\">");
                writer.WriteLine("<thead><tr><th scope=\"col\"><a>Categories</a></th></tr></thead><tbody>");

                foreach (var item in cats)
                {
                    writer.Write("<tr style=\"background: ");
                    writer.Write(item.Item1);
                    writer.Write(";\"><td><a>");
                    writer.Write(encoder.Encode(item.Item2));
                    writer.WriteLine("</a></td></tr>");
                }

                writer.WriteLine("</tbody></table>");
            }

            writer.WriteLine("<table id=\"cell_legend\" class=\"scoreboard scorelegend\">");
            writer.WriteLine("<thead><tr><th scope=\"col\">Cell colours</th></tr></thead><tbody>");
            writer.WriteLine("<tr class=\"score_first\"><td>Solved first</td></tr>");
            writer.WriteLine("<tr class=\"score_correct\"><td>Solved</td></tr>");
            writer.WriteLine("<tr class=\"score_incorrect\"><td>Tried, incorrect</td></tr>");
            writer.WriteLine("<tr class=\"score_pending\"><td>Tried, pending</td></tr>");
            writer.WriteLine("<tr class=\"score_neutral\"><td>Untried</td></tr>");
            writer.WriteLine("</tbody></table>");
        }
    }
}
