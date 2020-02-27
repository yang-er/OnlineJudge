using System;

namespace Microsoft.AspNetCore.Razor.Hosting
{
    internal class RazorCompiledItemLoader2 : RazorCompiledItemLoader
    {
        public string AreaName { get; }

        public RazorCompiledItemLoader2(string areaName)
        {
            AreaName = areaName;
        }

        protected override RazorCompiledItem CreateItem(RazorCompiledItemAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return new AreaRazorCompiledItem(attribute, AreaName);
        }
    }
}
