using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// Render badges with color
    /// </summary>
    [HtmlTargetElement("badge")]
    public class BadgeTagHelper : XysTagHelper
    {
        [HtmlAttributeName("color")]
        public BootstrapColor Color { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.AddClass($"badge badge-{Color}");
            base.Process(context, output);
        }
    }

    /// <summary>
    /// Render tags by badges with color
    /// </summary>
    [HtmlTargetElement("tags", TagStructure = TagStructure.WithoutEndTag)]
    public class TagsTagHelper : XysTagHelper
    {
        [HtmlAttributeName("color")]
        public BootstrapColor Color { get; set; }

        [HtmlAttributeName("list")]
        public string? Content { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            var tag = Content ?? "";

            foreach (var src in tag.Split(','))
            {
                if (string.IsNullOrEmpty(src)) continue;
                output.Content
                    .AppendHtml($"<span class=\"badge badge-{Color}\">")
                    .Append(src.Trim())
                    .AppendHtml("</span>\n");
            }

            base.Process(context, output);
        }
    }
}
