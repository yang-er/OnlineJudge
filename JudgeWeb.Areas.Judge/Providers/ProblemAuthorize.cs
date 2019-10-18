using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace JudgeWeb.Areas.Judge.Providers
{
    public class ProblemAuthorizeAttribute : Attribute, IActionFilter
    {
        private readonly string _token;

        public ProblemAuthorizeAttribute(string pidToken) => _token = pidToken;

        public void OnActionExecuted(ActionExecutedContext context) { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (!context.RouteData.Values.TryGetValue(_token, out var pid))
                context.Result = controller.BadRequest();
            if (controller.User.IsInRole("Administrator")) return;
            if ((string)pid == "add")
                context.Result = !controller.User.IsInRole("Problem") ? controller.Forbid() : null;
            else if (!controller.User.IsInRole("AuthorOfProblem" + (string)pid))
                context.Result = controller.Forbid();
        }
    }
}
