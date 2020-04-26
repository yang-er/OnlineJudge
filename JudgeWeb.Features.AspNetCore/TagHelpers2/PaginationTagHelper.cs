using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// Create <c>pagination</c> element
    /// </summary>
    [HtmlTargetElement("pagination", TagStructure = TagStructure.WithoutEndTag)]
    public class PaginationTagHelper : XysTagHelper
    {
        private IDictionary<string, string>? _routeValues;

        [HtmlAttributeName("bs-total-page")]
        public int? TotalPage { get; set; }

        [HtmlAttributeName("bs-current-page")]
        public int CurrentPage { get; set; }
        
        [HtmlAttributeName("asp-all-route-data", DictionaryAttributePrefix = "asp-route-")]
        public IDictionary<string, string> RouteValues
        {
            get => _routeValues ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            set => _routeValues = value;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
#pragma warning disable CS8618
        public ViewContext ViewContext { get; set; }
#pragma warning restore CS8618


        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "ul";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.AddClass("pagination pagination-sm");
            var url = ViewContext.HttpContext
                .RequestServices
                .GetRequiredService<IUrlHelperFactory>()
                .GetUrlHelper(ViewContext);
            var rv = RouteValues;
            output.Content.AppendHtml("\n");

            string? Link (int page) => url.Action(null,
                new RouteValueDictionary(rv) { ["page"] = $"{page}" });
            string DisableIf(int? i) => CurrentPage == i ? "disabled" : "";
            string ActiveIf(int i) => CurrentPage == i ? "active" : "";
            void Append(string ifs, string? link, string name) =>
                output.Content.AppendHtml(
                    $"<li class=\"page-item {ifs}\">" +
                        $"<a class=\"page-link\" href=\"{link}\">{name}</a>" +
                    "</li>\n");

            if (TotalPage.HasValue && TotalPage.Value <= 0)
            {
                Append("disabled", "#", "&laquo;");
                Append("disabled", "#", "&raquo;");
            }
            else if (TotalPage.HasValue)
            {
                Append(DisableIf(1), Link(CurrentPage - 1), "&laquo;");
                for (int i = 1; i <= TotalPage.Value; i++)
                    Append(ActiveIf(i), Link(i), $"{i}");
                Append(DisableIf(TotalPage), Link(CurrentPage + 1), "&raquo;");
            }
            else
            {
                Append(DisableIf(1), Link(CurrentPage - 1), "&laquo;");
                for (int i = Math.Max(1, CurrentPage - 2); i < CurrentPage; i++)
                    Append("", Link(i), $"{i}");
                Append("active", Link(CurrentPage), $"{CurrentPage}");
                for (int i = CurrentPage + 1; i < CurrentPage + 3; i++)
                    Append("", Link(i), $"{i}");
                Append("", Link(CurrentPage + 1), "&raquo;");
            }

            base.Process(context, output);
        }
    }
}
