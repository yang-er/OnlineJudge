using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("td", Attributes = "use-a")]
    public class TableCellDataUrlTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            output.Attributes.TryGetAttribute("use-a", out var usedTag);
            output.Attributes.Remove(usedTag);

            if (context.Items.TryGetValue("tr-url", out var trTag2)
                && trTag2 is TableRowDataUrlTagHelper trTag)
            {
                var item = new TagBuilder("a");
                item.Attributes.Add("href", trTag.DataUrl);
                item.AddCssClass("text-reset");

                if (trTag.AjaxWindow != null)
                {
                    item.Attributes.Add("data-toggle", "ajaxWindow");
                    item.Attributes.Add("data-target", trTag.AjaxWindow);
                }
                
                output.PreContent.AppendHtml(item.RenderStartTag());
                output.PostContent.AppendHtml(item.RenderEndTag());
            }
        }
    }
}
