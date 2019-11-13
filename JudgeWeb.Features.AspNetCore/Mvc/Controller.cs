using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc
{
    public abstract class Controller2 : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            string handlekey = null;
            if (context.HttpContext.Request.Query.TryGetValue(nameof(handlekey), out var qs))
                handlekey = qs.FirstOrDefault();
            else if (context.HttpContext.Request.HasFormContentType
                    && context.HttpContext.Request.Form.TryGetValue(nameof(handlekey), out qs))
                handlekey = qs.FirstOrDefault();

            IsWindowAjax = handlekey != null;

            if (IsWindowAjax || (HttpContext.Request.Headers.TryGetValue("X-Requested-With", out var val)
                    && val.FirstOrDefault() == "XMLHttpRequest"))
                InAjax = true;

            if (InAjax)
            {
                ViewData["InAjax"] = true;
            }

            if (IsWindowAjax)
            {
                ViewData["HandleKey"] = handlekey;
                ViewData["HandleKey2"] = System.Guid.NewGuid().ToString().Substring(0, 6);
            }
        }

        [NonAction]
        public ContentFileResult ContentFile(
            string fileName, string contentType, string downloadName)
        {
            return new ContentFileResult(fileName, contentType)
            {
                FileDownloadName = downloadName
            };
        }

        [NonAction]
        public ShowMessageResult Message(string title, string message, MessageType? type = null)
        {
            return new ShowMessageResult
            {
                ViewData = ViewData,
                TempData = TempData,
                Title = title,
                Content = message,
                Type = type,
            };
        }

        [NonAction]
        public ShowMessage2Result AskPost(
            string title, string message,
            string area, string ctrl, string act,
            Dictionary<string, string> routeValues = null,
            MessageType? type = null)
        {
            return new ShowMessage2Result
            {
                ViewData = ViewData,
                TempData = TempData,
                Title = title,
                Content = message,
                Type = type,
                AreaName = area,
                ControllerName = ctrl,
                ActionName = act,
                RouteValues = routeValues,
            };
        }

        [NonAction]
        public ShowWindowResult Window()
        {
            return Window(null, null);
        }

        [NonAction]
        public ShowWindowResult Window(object model)
        {
            return Window(null, model);
        }

        [NonAction]
        public ShowWindowResult Window(string viewName)
        {
            return Window(viewName, null);
        }

        [NonAction]
        public ShowWindowResult Window(string viewName, object model)
        {
            ViewData.Model = model;

            return new ShowWindowResult
            {
                ViewName = viewName,
                ViewData = ViewData,
                TempData = TempData,
            };
        }

        public bool InAjax { get; private set; }

        public bool IsWindowAjax { get; private set; }
    }
}
