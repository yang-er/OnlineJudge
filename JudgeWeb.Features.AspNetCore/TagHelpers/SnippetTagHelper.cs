using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("snippet")]
    public class SnippetTagHelper : TagHelper
    {
        const string deleted = "Record has been deleted.";

        [HtmlAttributeName("base64")]
        public string Base64Source { get; set; }

        [HtmlAttributeName("filename")]
        public string FileName { get; set; }

        [HtmlAttributeName("nodata")]
        public string NoData { get; set; }

        [HtmlAttributeName("h5-title")]
        public string Header5 { get; set; }

        private static (bool, string) ConvertBase64(string b64, string nodata)
        {
            try
            {
                var values = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                if (string.IsNullOrWhiteSpace(values)) return (false, nodata);
                return (true, values);
            }
            catch
            {
                return (false, "Error while parsing " + b64 + " . Please contact XiaoYang.");
            }
        }

        private static async ValueTask<(bool, string)> ReadFileAsync(string filename, string nodata)
        {
            if (!System.IO.File.Exists(filename)) return (false, deleted);
            var sb = new StringBuilder();

            using (var sr = new System.IO.StreamReader(filename))
            {
                var arr = new char[1024];
                var len = await sr.ReadBlockAsync(arr, 0, 1024);
                sb.Append(arr, 0, len);
                if (len >= 1024) sb.Append("...\n[content display truncated after 1024B]");
            }

            var content = sb.ToString();
            if (string.IsNullOrWhiteSpace(content)) return (false, nodata);
            return (true, content);
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            string result, append_class; bool ok;

            if (Base64Source != null)
                (ok, result) = ConvertBase64(Base64Source, NoData);
            else
                (ok, result) = await ReadFileAsync(FileName, NoData);

            if (!string.IsNullOrWhiteSpace(Header5) && result == deleted)
            {
                output.TagName = null;
                return;
            }

            if (ok)
            {
                output.TagName = "pre";
                append_class = "output_text";
            }
            else
            {
                output.TagName = "p";
                append_class = "nodata";
            }

            if (output.Attributes.ContainsName("class"))
                append_class += " " + output.Attributes["class"].Value;
            output.Attributes.SetAttribute("class", append_class);
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Content.Append(result);

            if (!string.IsNullOrWhiteSpace(Header5))
                output.PreContent.AppendHtml("<h5>").Append(Header5).AppendHtml("</h5>");
        }
    }
}
