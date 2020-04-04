using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Mvc
{
    public static class Extensions
    {
        public static AuthenticationBuilder SetCookie(
            this AuthenticationBuilder builder,
            Action<CookieAuthenticationOptions> configure)
        {
            builder.Services.ConfigureApplicationCookie(configure);
            return builder;
        }

        public static bool IsInRoles(this ClaimsPrincipal user, string roles)
        {
            return roles.Split(',').Any(role => user.IsInRole(role));
        }

        public static string GetUserName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name");
        }

        public static string GetUserId(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        }

        public static IApplicationBuilder EnsureClaimTypes(this IApplicationBuilder app)
        {
            var optionsAccessor = app.ApplicationServices.GetRequiredService<IOptions<IdentityOptions>>();
            if (optionsAccessor.Value.ClaimsIdentity.UserIdClaimType != ClaimTypes.NameIdentifier
                && optionsAccessor.Value.ClaimsIdentity.UserIdClaimType != "sub")
                throw new InvalidOperationException();
            return app;
        }

        public static string GetNickName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue("nickname") ?? GetUserName(principal);
        }

        public static T Deserialize<T>(this IJsonHelper jsonHelper, string content)
        {
            return JsonSerializer.Deserialize<T>(content);
        }

        public static string GetErrorStrings(this ModelStateDictionary modelState)
        {
            var sb = new StringBuilder();
            foreach (var state in modelState)
            {
                if (state.Value.ValidationState != ModelValidationState.Invalid) continue;
                foreach (var item in state.Value.Errors)
                    sb.AppendLine(item.ErrorMessage);
            }

            return sb.ToString();
        }
    }
}
