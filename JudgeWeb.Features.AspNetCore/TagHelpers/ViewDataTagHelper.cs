using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("viewmodule")]
    public class ViewDataTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        [HtmlAttributeName("key")]
        public string Key { get; set; }

        [HtmlAttributeName("roles")]
        public string Roles { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            output.TagName = null;
            bool suppress = true;
            if (Key != null && ViewContext.ViewData.ContainsKey(Key))
                suppress = false;
            if (Roles != null && ViewContext.HttpContext.User.IsInRoles(Roles))
                suppress = false;
            if (suppress)
                output.SuppressOutput();
        }
    }
}
