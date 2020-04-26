using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("ace")]
    public class AceEditorTagHelper : XysTagHelper
    {
        private IHtmlHelper HtmlHelper { get; set; }

        [HtmlAttributeName("file")]
        public string File { get; set; }

        [HtmlAttributeName("value")]
        public string Content { get; set; }

        [HtmlAttributeNotBound]
        public string RandomId { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public AceEditorTagHelper(IHtmlHelper htmlHelper)
        {
            HtmlHelper = htmlHelper;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            RandomId = Guid.NewGuid().ToString().Substring(0, 6);
            (HtmlHelper as IViewContextAware).Contextualize(ViewContext);
            ViewContext.ViewData["AceEditor"] = true;
            output.Content.SetHtmlContent(await HtmlHelper.PartialAsync("_AceEditor", this));
        }
    }
}
