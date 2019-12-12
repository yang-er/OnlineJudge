using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Mvc
{
    public static class RoutingExtensions
    {
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
