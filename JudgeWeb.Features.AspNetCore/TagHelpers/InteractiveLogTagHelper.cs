using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("interactive")]
    public class InteractiveLogTagHelper : TagHelper
    {
        [HtmlAttributeName("filename")]
        public string FileName { get; set; }

        private static void ParseLog(ReadOnlySpan<char> log, TagHelperContent sb)
        {
            int idx = 0;
            while (idx < log.Length)
            {
                int slashPos = log.Slice(idx).IndexOf('/');
                if (slashPos == -1) break; else slashPos += idx;
                string time = new string(log.Slice(idx + 1, slashPos - idx - 1));
                idx = slashPos + 1;
                int closePos = log.Slice(idx).IndexOf(']');
                if (closePos == -1) break; else closePos += idx;
                int len = int.Parse(log.Slice(idx, closePos - idx));
                if (closePos + 4 + len >= log.Length) break;
                idx = closePos + 1;
                bool is_validator = log[idx] == '>';

                sb.AppendHtml("<tr><td>").Append(time);
                if (!is_validator) sb.AppendHtml("</td><td>");
                sb.AppendHtml("</td><td class=\"output_text\">");
                var str = log.Slice(idx + 3, len); int igx;

                while ((igx = str.IndexOf('\n')) != -1)
                {
                    sb.Append(new string(str.Slice(0, igx))).Append("\u21B5").AppendHtml("<br/>");
                    str = str.Slice(igx + 1);
                }

                if (str.Length > 0) sb.Append(new string(str));
                if (is_validator) sb.AppendHtml("</td><td>");
                sb.AppendHtml("</td></tr>");
                idx += len + 4;
            }
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!System.IO.File.Exists(FileName))
            {
                output.TagName = "p";
                output.TagMode = TagMode.StartTagAndEndTag;
                output.Attributes.Add("class", "nodata");
                output.Content.Append("Record has been deleted.");
                return;
            }

            int len = 0;
            var arr = new char[2000];
            using (var sr = new System.IO.StreamReader(FileName))
                len = await sr.ReadBlockAsync(arr, 0, 2000);

            output.TagName = null;
            output.Content.AppendHtml("<table><tr><th>time</th><th>validator</th><th>submission<th></tr>\n");
            ParseLog(new ReadOnlySpan<char>(arr, 0, len), output.Content);
            output.Content.AppendHtml("</table>\n");
            if (len >= 2000)
                output.Content.Append("[content display truncated after 2000B]");
        }
    }
}
