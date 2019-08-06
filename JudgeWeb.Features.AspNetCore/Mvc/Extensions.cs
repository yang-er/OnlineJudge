using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace Microsoft.AspNetCore.Mvc
{
    public static class Extensions
    {
        public static bool IsInRoles(this ClaimsPrincipal user, string roles)
        {
            return roles.Split(',').Any(role => user.IsInRole(role));
        }

        public static IMvcBuilder SetTokenTransform<T>(this IMvcBuilder builder)
            where T : IOutboundParameterTransformer, new()
        {
            builder.Services.Configure<MvcOptions>(options =>
                options.Conventions.Add(
                    new RouteTokenTransformerConvention(new T())));
            return builder;
        }

        public static IMvcBuilder EnableContentFileResult(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton<
                IActionResultExecutor<ContentFileResult>,
                ContentFileResultExecutor>();
            return builder;
        }

        private static bool TryLoad(string assemblyName, out Assembly assembly)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + assemblyName))
            {
                assembly = Assembly.LoadFrom(AppDomain.CurrentDomain.BaseDirectory + assemblyName);
                return true;
            }
            else
            {
                assembly = null;
                return false;
            }
        }

        public static IdentityBuilder UsePasswordHasher<THasher, TUser>(this IdentityBuilder builder)
            where THasher : class, IPasswordHasher<TUser> where TUser : class
        {
            builder.Services.Replace(ServiceDescriptor.Scoped<IPasswordHasher<TUser>, THasher>());
            return builder;
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

        public static IMvcBuilder UseAreaParts(this IMvcBuilder builder,
            string projectPrefix, IEnumerable<string> areaNames)
        {
#if DEBUG
            // only in debug mode should we open this module
            // so that we can hot-fix our cshtml.
            builder.Services.AddOptions<RazorViewEngineOptions>()
                .Configure<IHostingEnvironment>((options, env) =>
                {
                    var areaProvider = new AreasFileProvider(
                        new PhysicalFileProvider(env.ContentRootPath + "/../"),
                        projectPrefix ?? throw new ArgumentNullException("ProjectPrefix"));
                    areaProvider.AddExternalAreas(areaNames ?? Enumerable.Empty<string>());
                    options.FileProviders.Add(areaProvider);
                });
#endif

            builder.ConfigureApplicationPartManager(apm =>
            {
                foreach (var area in areaNames ?? Enumerable.Empty<string>())
                {
                    if (TryLoad(projectPrefix + area + ".dll", out var assembly1))
                    {
                        apm.ApplicationParts.Add(new AssemblyPart(assembly1));
                        foreach (var attr in assembly1.GetCustomAttributes<InjectAttribute>())
                            builder.Services.TryAdd(attr.GetDescriptior());
                    }

                    if (TryLoad(projectPrefix + area + ".Views.dll", out var assembly2))
                    {
                        apm.ApplicationParts.Add(new AreaRazorAssemblyPart(assembly2, area));
                    }
                }
            });

            return builder;
        }
    }
}
