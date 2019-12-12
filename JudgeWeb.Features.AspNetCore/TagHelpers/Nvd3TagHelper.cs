using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("nvd3")]
    public class Nvd3TagHelper : TagHelper
    {
        private IHtmlHelper HtmlHelper { get; set; }

        [HtmlAttributeName("y-axis")]
        public string YAxis { get; set; }

        [HtmlAttributeName("id")]
        public string Id { get; set; }

        [HtmlAttributeName("title")]
        public string Title { get; set; }

        [HtmlAttributeName("baseline")]
        public double Baseline { get; set; }

        [HtmlAttributeName("max-value")]
        public double MaxValue { get; set; }

        [HtmlAttributeName("x-axis")]
        public string XAxis { get; set; }

        [HtmlAttributeName("key")]
        public string KeyName { get; set; }

        [HtmlAttributeName("data")]
        public IEnumerable<object> DataObject { get; set; }

        [HtmlAttributeNotBound]
        public string GeneratedValue { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public Nvd3TagHelper(IHtmlHelper htmlHelper)
        {
            HtmlHelper = htmlHelper;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.Add("style", "display: inline-block");
            output.Attributes.Add("id", Id);
            GeneratedValue = new { key = KeyName, values = DataObject }.ToJson();
            (HtmlHelper as IViewContextAware).Contextualize(ViewContext);
            ViewContext.ViewData["Nvd3"] = true;
            output.Content.SetHtmlContent(await HtmlHelper.PartialAsync("_Nvd3", this));
        }
    }
}
