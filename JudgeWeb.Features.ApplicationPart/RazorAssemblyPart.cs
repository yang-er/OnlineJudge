using Microsoft.AspNetCore.Razor.Hosting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public class AreaRazorAssemblyPart : ApplicationPart, IRazorCompiledItemProvider
    {
        public AreaRazorAssemblyPart(Assembly assembly, string areaName)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            AreaName = areaName;
        }

        public string AreaName { get; }

        public Assembly Assembly { get; }

        public override string Name => Assembly.GetName().Name;

        IEnumerable<RazorCompiledItem> IRazorCompiledItemProvider.CompiledItems
        {
            get
            {
                var loader = new RazorCompiledItemLoader2(AreaName);
                return loader.LoadItems(Assembly);
            }
        }
    }
}
