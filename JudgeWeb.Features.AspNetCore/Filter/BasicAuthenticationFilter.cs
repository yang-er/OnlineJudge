using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 通用 Basic Authorization 过滤器
    /// </summary>
    public class BasicAuthenticationFilter : Attribute, IAuthorizationFilter
    {
        private ISet<string> AuthorizeString { get; set; }

        public string RealmName { get; }

        public BasicAuthenticationFilter(string realmName, params string[] vs)
        {
            RealmName = realmName;
            AuthorizeString = new HashSet<string>(vs);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var auth = ParseHeader(context.HttpContext.Request);

            if (auth is null)
            {
                context.Result = new NeedAuthenticationResult(RealmName);
            }
            else if (!AuthorizeString.Contains(auth))
            {
                context.Result = new StatusCodeResult(401);
            }
        }

        protected virtual string ParseHeader(HttpRequest request)
        {
            try
            {
                request.Headers.TryGetValue("Authorization", out var auth);
                if (auth.Count != 1) return null;
                var authString = auth.First();
                if (!authString.StartsWith("Basic ")) return null;
                return authString.Substring(6).UnBase64();
            }
            catch
            {
                return null;
            }
        }

        private class NeedAuthenticationResult : ActionResult
        {
            const string HeaderName = "WWW-Authenticate";
            const string HeaderValue = "Basic realm=\"{0}\"";

            public string Realm { get; }

            public NeedAuthenticationResult(string realm)
            {
                Realm = realm;
            }

            public override void ExecuteResult(ActionContext context)
            {
                base.ExecuteResult(context);
                context.HttpContext.Response.Headers.Add(HeaderName, string.Format(HeaderValue, Realm));
                context.HttpContext.Response.StatusCode = 401;
            }
        }
    }
}
