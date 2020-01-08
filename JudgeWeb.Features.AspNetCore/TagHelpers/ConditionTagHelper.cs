using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("if")]
    public class ConditionTagHelper : TagHelper
    {
        [HtmlAttributeName("cond")]
        public bool Condition { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            if (!Condition) output.SuppressOutput();
            else output.TagName = null;
        }
    }
}
