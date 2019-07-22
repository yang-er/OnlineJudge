using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Hosting
{
    internal class AreaRazorCompiledItem : RazorCompiledItem
    {
        public override string Identifier { get; }

        public override string Kind { get; }

        public override IReadOnlyList<object> Metadata { get; }

        public override Type Type { get; }

        public AreaRazorCompiledItem(RazorCompiledItemAttribute attr, string areaName)
        {
            Type = attr.Type;
            Kind = attr.Kind;
            Identifier = "/Areas/" + areaName + attr.Identifier;

            Metadata = Type.GetCustomAttributes(inherit: true).Select(o =>
                o is RazorSourceChecksumAttribute rsca
                    ? new RazorSourceChecksumAttribute(rsca.ChecksumAlgorithm, rsca.Checksum, "/Areas/" + areaName + rsca.Identifier)
                    : o).ToList();
        }
    }
}
