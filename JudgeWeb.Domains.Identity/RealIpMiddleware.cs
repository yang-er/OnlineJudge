using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public class RealIpMiddleware
    {
        private readonly RequestDelegate _next;

        public RealIpMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            var headers = context.Request.Headers;

            context.Request.Host = new HostString("acm.xylab.fun");
            context.Request.Scheme = "https";
            
            if (headers.ContainsKey("Ali-Cdn-Real-Ip"))
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(headers["Ali-Cdn-Real-Ip"]);
            }

            return _next(context);
        }
    }
}
