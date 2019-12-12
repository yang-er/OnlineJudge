using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    public class RedirectToActionWithAjaxSupportResult : ActionResult, IKeepTempDataResult
    {
        public RedirectToActionWithAjaxSupportResult(RedirectToActionResult src, bool inajax)
        {
            InAjax = inajax;
            ActionName = src.ActionName;
            ControllerName = src.ControllerName;
            RouteValues = src.RouteValues;
            PreserveMethod = src.PreserveMethod;
            Fragment = src.Fragment;
        }

        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public RouteValueDictionary RouteValues { get; set; }
        public bool PreserveMethod { get; set; }
        public string Fragment { get; set; }
        public bool InAjax { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            ExecuteResult(context);
            return Task.CompletedTask;
        }

        public override void ExecuteResult(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var urlHelper = context.HttpContext.RequestServices
                .GetRequiredService<IUrlHelperFactory>()
                .GetUrlHelper(context);

            var destinationUrl = urlHelper.Action(
                ActionName,
                ControllerName,
                RouteValues,
                protocol: null,
                host: null,
                fragment: Fragment);

            if (string.IsNullOrEmpty(destinationUrl))
                throw new InvalidOperationException("No Routes Matched");

            if (InAjax)
            {
                context.HttpContext.Response.StatusCode =
                    StatusCodes.Status200OK;
                context.HttpContext.Response.Headers["X-Login-Page"] = destinationUrl;
            }
            else
            {
                context.HttpContext.Response.StatusCode = PreserveMethod
                    ? StatusCodes.Status307TemporaryRedirect
                    : StatusCodes.Status302Found;
                context.HttpContext.Response.Headers[HeaderNames.Location] = destinationUrl;
            }
        }
    }
}
