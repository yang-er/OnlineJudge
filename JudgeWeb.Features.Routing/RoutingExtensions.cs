using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{
    public static class RoutingExtensions
    {
        public static IMvcBuilder ReplaceLinkGenerator(this IMvcBuilder mvc)
        {
            var old = mvc.Services.FirstOrDefault(s => s.ServiceType == typeof(LinkGenerator));
            OrderLinkGenerator.typeInner = old.ImplementationType;
            mvc.Services.Replace(ServiceDescriptor.Singleton<LinkGenerator, OrderLinkGenerator>());
            return mvc;
        }

        public static IMvcBuilder SetTokenTransform<T>(this IMvcBuilder builder)
            where T : IOutboundParameterTransformer, new()
        {
            builder.Services.Configure<MvcOptions>(options =>
                options.Conventions.Add(
                    new RouteTokenTransformerConvention(new T())));
            return builder;
        }
    }
}
