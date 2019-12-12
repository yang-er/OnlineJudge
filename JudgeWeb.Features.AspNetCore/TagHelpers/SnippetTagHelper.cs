using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Text;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("snippet")]
    public class SnippetTagHelper : TagHelper
    {
        [HtmlAttributeName("base64")]
        public string Base64Source { get; set; }

        private static string ConvertBase64(string b64)
        {
            try
            {
                var values = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                if (string.IsNullOrWhiteSpace(values)) values = "No content.";
                return values;
            }
            catch
            {
                return "Error while parsing " + b64 + " . Please contact XiaoYang.";
            }
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);

            output.TagName = "pre";
            output.TagMode = TagMode.StartTagAndEndTag;
            
            if (output.Attributes.ContainsName("class"))
            {
                var newClass = "output_text " + output.Attributes["class"].Value;
                output.Attributes.SetAttribute("class", newClass);
            }
            else
            {
                output.Attributes.SetAttribute("class", "output_text");
            }

            output.Content.Append(ConvertBase64(Base64Source));
        }
    }
}
