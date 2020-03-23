using JudgeWeb;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Microsoft.AspNetCore.Mvc
{
    public sealed class AuditPointAttribute : Attribute, IActionFilter
    {
        private readonly AuditlogType _type;

        public AuditPointAttribute(AuditlogType type) => _type = type;

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Items.Add(nameof(AuditlogType), _type);
        }
    }
}
