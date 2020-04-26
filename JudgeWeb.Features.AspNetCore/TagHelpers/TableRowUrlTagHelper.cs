using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("tr", Attributes = ActionAttributeName)]
    [HtmlTargetElement("tr", Attributes = ControllerAttributeName)]
    [HtmlTargetElement("tr", Attributes = AreaAttributeName)]
    [HtmlTargetElement("tr", Attributes = PageAttributeName)]
    [HtmlTargetElement("tr", Attributes = PageHandlerAttributeName)]
    [HtmlTargetElement("tr", Attributes = FragmentAttributeName)]
    [HtmlTargetElement("tr", Attributes = HostAttributeName)]
    [HtmlTargetElement("tr", Attributes = ProtocolAttributeName)]
    [HtmlTargetElement("tr", Attributes = RouteAttributeName)]
    [HtmlTargetElement("tr", Attributes = RouteValuesDictionaryName)]
    [HtmlTargetElement("tr", Attributes = RouteValuesPrefix + "*")]
    public class TableRowDataUrlTagHelper : AnchorTagHelper
    {
        private const string ActionAttributeName = "asp-action";
        private const string ControllerAttributeName = "asp-controller";
        private const string AreaAttributeName = "asp-area";
        private const string PageAttributeName = "asp-page";
        private const string PageHandlerAttributeName = "asp-page-handler";
        private const string FragmentAttributeName = "asp-fragment";
        private const string HostAttributeName = "asp-host";
        private const string ProtocolAttributeName = "asp-protocol";
        private const string RouteAttributeName = "asp-route";
        private const string RouteValuesDictionaryName = "asp-all-route-data";
        private const string RouteValuesPrefix = "asp-route-";

        public TableRowDataUrlTagHelper(IHtmlGenerator generator) : base(generator)
        {
        }

        [HtmlAttributeNotBound]
        public string DataUrl { get; set; }

        [HtmlAttributeNotBound]
        public string AjaxWindow { get; set; }

        [HtmlAttributeName("asp-link-ignore")]
        public bool Ignore { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items.Add("tr-url", this);
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);

            // move `href` into `data-url`
            output.Attributes.TryGetAttribute("href", out var attr);
            output.Attributes.Remove(attr);
            DataUrl = (string)attr.Value;

            if (output.Attributes.TryGetAttribute("data-toggle", out var dataToggle))
            {
                output.Attributes.Remove(dataToggle);

                if ("ajaxWindow" == ((HtmlString)dataToggle.Value).Value)
                {
                    output.Attributes.TryGetAttribute("data-target", out var dataTarget);
                    output.Attributes.Remove(dataTarget);
                    AjaxWindow = ((HtmlString)dataTarget.Value).Value;
                }
            }
        }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (DisplayWhenTagHelper.Check(context))
                return Task.CompletedTask;
            return base.ProcessAsync(context, output);
        }
    }

    [HtmlTargetElement("td", Attributes = "use-a")]
    public class TableCellDataUrlTagHelper : XysTagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            output.Attributes.TryGetAttribute("use-a", out var usedTag);
            output.Attributes.Remove(usedTag);

            if (context.Items.TryGetValue("tr-url", out var trTag2)
                && trTag2 is TableRowDataUrlTagHelper trTag && !trTag.Ignore)
            {
                var item = new TagBuilder("a");
                item.Attributes.Add("href", trTag.DataUrl);
                string @class = usedTag.ValueStyle == HtmlAttributeValueStyle.Minimized ? "text-reset text-decoration-none" : usedTag.Value.ToString();
                item.AddCssClass(@class);

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
