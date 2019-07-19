using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Mvc
{
    public static class Extensions
    {
        public static bool IsInRoles(this ClaimsPrincipal user, string roles)
        {
            return roles.Split(',').Any(role => user.IsInRole(role));
        }

        public static IMvcBuilder AddMvc2(this IServiceCollection services)
        {
            Action<MvcOptions> conf = options =>
            {
                options.Conventions.Add(
                    new RouteTokenTransformerConvention(
                        new SlugifyParameterTransformer()));
            };

            var builder = services.AddMvc(conf);
            services.TryAddSingleton<IActionResultExecutor<ContentFileResult>, ContentFileResultExecutor>();
            builder.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            return builder;
        }
    }
}
