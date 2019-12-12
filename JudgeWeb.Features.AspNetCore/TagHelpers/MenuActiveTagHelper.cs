using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Features.Razor
{
    [HtmlTargetElement("a", Attributes = ActiveCtrlName)]
    [HtmlTargetElement("a", Attributes = ActiveActName)]
    [HtmlTargetElement("a", Attributes = ActiveAreaName)]
    [HtmlTargetElement("a", Attributes = ActiveViewDataName)]
    public class MenuActiveTagHelper : TagHelper
    {
        private const string ActiveViewDataName = "active-vd";
        private const string ActiveCtrlName = "active-ctrl";
        private const string ActiveActName = "active-act";
        private const string ActiveAreaName = "active-area";

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        [HtmlAttributeName(ActiveCtrlName)]
        public string ActiveController { get; set; }

        [HtmlAttributeName(ActiveActName)]
        public string ActiveAction { get; set; }

        [HtmlAttributeName(ActiveViewDataName)]
        public string ActiveViewData { get; set; }

        [HtmlAttributeName(ActiveAreaName)]
        public string ActiveArea { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);

            bool setActive = false;

            if (ActiveController != null &&
                ViewContext.ActionDescriptor is ControllerActionDescriptor cad &&
                ActiveController.Split(',').Contains(cad.ControllerName))
                setActive = true;

            if (ActiveAction != null &&
                ViewContext.ActionDescriptor is ControllerActionDescriptor cad2 &&
                ActiveAction.Split(',').Contains(cad2.ActionName))
                setActive = true;

            if (ActiveArea != null &&
                (string)ViewContext.RouteData.Values.GetValueOrDefault("area") == ActiveArea)
                setActive = true;

            if (ActiveViewData != null &&
                (string)ViewContext.ViewData["ActiveAction"] == ActiveViewData)
                setActive = true;

            if (setActive)
            {
                output.Attributes.TryGetAttribute("class", out var attrs);
                var nowClass = (attrs?.Value ?? "").ToString();
                nowClass += " active";
                output.Attributes.SetAttribute("class", nowClass);
            }
        }
    }
}
