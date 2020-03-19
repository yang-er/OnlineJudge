using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Identity.Providers
{
    public class CookieAuthenticationValidator : CookieAuthenticationEvents
    {
        private static IMemoryCache SlideExpireMemoryCache =>
            Microsoft.EntityFrameworkCore.CachedQueryable.Cache;

        public CookieAuthenticationValidator()
        {
            OnValidatePrincipal = ValidatePrincipalImpl;
            OnRedirectToLogin = c => RedirectImpl(c, 401);
            OnRedirectToAccessDenied = c => RedirectImpl(c, 403);
            OnRedirectToLogout = c => RedirectImpl(c, null);
            OnRedirectToReturnUrl = c => RedirectImpl(c, null);
        }

        private static async Task ValidatePrincipalImpl(CookieValidatePrincipalContext context)
        {
            var um = context.HttpContext.RequestServices
                .GetRequiredService<UserManager>();
            var userName = um.GetUserName(context.Principal);
            DateTimeOffset? orig = context.Properties.IssuedUtc;

            if (userName != null)
            {
                userName = um.NormalizeName(userName);
                if (SlideExpireMemoryCache.TryGetValue("SlideExpiration: " + userName, out DateTimeOffset last)
                    && last > context.Properties.IssuedUtc)
                    context.Properties.IssuedUtc = null;
            }

            await SecurityStampValidator.ValidatePrincipalAsync(context);
            context.Properties.IssuedUtc = orig;
        }

        private static Task RedirectImpl(RedirectContext<CookieAuthenticationOptions> context, int? statusCode)
        {
            if (IsAjaxRequest(context.Request))
            {
                context.Response.Headers["X-Login-Page"] = context.RedirectUri;
                if (statusCode.HasValue)
                    context.Response.StatusCode = statusCode.Value;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }

            return Task.CompletedTask;
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            return string.Equals(request.Query["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal) ||
                string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);
        }
    }
}
