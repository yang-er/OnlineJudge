using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ValidateInAjaxAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Here is nothing to do.
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller2;
            if (controller is null) throw new InvalidOperationException();
            if (!controller.IsWindowAjax) context.Result = controller.BadRequest();
        }
    }
}
