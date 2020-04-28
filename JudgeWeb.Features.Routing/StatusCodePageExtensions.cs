using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    public static class MyStatusCodePageExtensions
    {
        private static async Task Handle(StatusCodeContext context, string _newPath)
        {
            var newPath = new PathString(_newPath);
            var newQueryString = QueryString.Empty;

            var originalPath = context.HttpContext.Request.Path;
            var originalQueryString = context.HttpContext.Request.QueryString;

            // Store the original paths so the app can check it.
            context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature()
            {
                OriginalPathBase = context.HttpContext.Request.PathBase.Value,
                OriginalPath = originalPath.Value,
                OriginalQueryString = originalQueryString.HasValue ? originalQueryString.Value : null,
            });

            // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
            // the endpoint and route values to ensure things are re-calculated.
            context.HttpContext.SetEndpoint(endpoint: null);
            var routeValuesFeature = context.HttpContext.Features.Get<IRouteValuesFeature>();
            routeValuesFeature?.RouteValues?.Clear();
            context.HttpContext.Request.Path = newPath;
            context.HttpContext.Request.QueryString = newQueryString;

            try
            {
                await context.Next(context.HttpContext);
            }
            finally
            {
                context.HttpContext.Request.QueryString = originalQueryString;
                context.HttpContext.Request.Path = originalPath;
                context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(null);
            }
        }

        public static void MapFallbackNotFound(this IEndpointRouteBuilder endpoints, string pattern)
        {
            endpoints.MapFallback(pattern, context =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });
        }

        public class CatchExceptionMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly ILogger<CatchExceptionMiddleware> _logger;
            private readonly DiagnosticSource _diagnosticSource;
            private const string _une = "Microsoft.AspNetCore.Diagnostics.UnhandledException";

            public CatchExceptionMiddleware(RequestDelegate next,
                ILogger<CatchExceptionMiddleware> logger,
                DiagnosticSource diagnosticSource)
            {
                _next = next;
                _logger = logger;
                _diagnosticSource = diagnosticSource;
            }

            public async Task Invoke(HttpContext context)
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
        }

        public static IApplicationBuilder UseCatchException(this IApplicationBuilder app)
        {
            app.UseMiddleware<CatchExceptionMiddleware>();
            return app;
        }

        public static IApplicationBuilder UseStatusCodePage(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            return app.UseStatusCodePages((StatusCodeContext context) =>
            {
                if (context.HttpContext.Request.Headers.TryGetValue("X-Requested-With", out var s)
                    && s.First() == "XMLHttpRequest")
                    return Task.CompletedTask;

                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/api"))
                    return Task.CompletedTask;
                if (path.StartsWithSegments("/images"))
                    return Task.CompletedTask;
                if (path.StartsWithSegments("/static"))
                    return Task.CompletedTask;

                if (context.HttpContext.Response.StatusCode != 404)
                {
                    var routeVal = context.HttpContext.Request.RouteValues;
                    if (path.StartsWithSegments("/dashboard"))
                        return Handle(context, "/dashboard/error");
                    if (path.StartsWithSegments("/polygon") && routeVal.ContainsKey("pid"))
                        return Handle(context, $"/polygon/{routeVal["pid"]}/error");
                    if (path.StartsWithSegments("/gym") && routeVal.ContainsKey("cid"))
                        return Handle(context, $"/gym/{routeVal["cid"]}/error");

                    if (path.StartsWithSegments("/contest") && routeVal.ContainsKey("cid"))
                    {
                        var temp = $"/contest/{routeVal["cid"]}";
                        if (path.StartsWithSegments(temp + "/jury"))
                            return Handle(context, $"/contest/{routeVal["cid"]}/jury/error");
                        return Handle(context, $"/contest/{routeVal["cid"]}/error");
                    }
                }

                return Handle(context, "/error");
            });
        }
    }
}
