using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ApplicationPartsExtensions
    {
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
