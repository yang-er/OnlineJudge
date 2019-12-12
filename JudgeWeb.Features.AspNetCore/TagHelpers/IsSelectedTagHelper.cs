using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("option", Attributes = "issel")]
    [HtmlTargetElement("input", Attributes = "issel")]
    public class IsSelectedTagHelper : TagHelper
    {
        [HtmlAttributeName("issel")]
        public bool IsSelected { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            if (IsSelected) output.Attributes.Add(
                name: output.TagName == "option" ? "selected" : "checked",
                value: "");
        }
    }
}
