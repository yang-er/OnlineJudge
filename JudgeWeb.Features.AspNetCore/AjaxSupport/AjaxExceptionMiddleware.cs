using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public class AjaxExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AjaxExceptionMiddleware> _logger;
        private readonly DiagnosticSource _diagnosticSource;
        private const string _une = "Microsoft.AspNetCore.Diagnostics.UnhandledException";
        
        public AjaxExceptionMiddleware(RequestDelegate next,
            ILogger<AjaxExceptionMiddleware> logger,
            DiagnosticSource diagnosticSource)
        {
            _next = next;
            _logger = logger;
            _diagnosticSource = diagnosticSource;
        }

        private async Task InvokeInAjax(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred while executing the request.");

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response has been started, rethrowing...");
                    throw;
                }

                try
                {
                    context.Response.Clear();
                    context.Response.StatusCode = 500;
                    await DisplayException(context, ex);
                    if (_diagnosticSource.IsEnabled(_une))
                        _diagnosticSource.Write(_une, new { httpContext = context, exception = ex });
                    return;
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Error generating responses.");
                }

                throw;
            }
        }

        private Task DisplayException(HttpContext context, Exception ex)
        {
            return context.Response.WriteAsync(
                "An unhandled exception occurred while processing the request.\n\n" +
                ex.ToString());
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Requested-With", out var s)
                && s.First() == "XMLHttpRequest")
                return InvokeInAjax(context);
            return _next(context);
        }
    }
}
