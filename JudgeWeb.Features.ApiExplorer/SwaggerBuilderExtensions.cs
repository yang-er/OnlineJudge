using JudgeWeb.Features.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class SwaggerBuilderExtensions
    {
        public static ApiExplorerBuilder AddApiExplorer(this IServiceCollection services, Action<SwaggerGenOptions> options = null)
        {
            return new ApiExplorerBuilder(services, options);
        }

        private static IEndpointConventionBuilder MapSwagger(this IEndpointRouteBuilder endpoints, string template, bool asV2)
        {
            var genOptions = endpoints.ServiceProvider
                .GetRequiredService<IOptions<SwaggerGenOptions>>();
            var dataSource = new SwaggerEndpointDataSource(genOptions, template, asV2);
            endpoints.DataSources.Add(dataSource);
            return dataSource;
        }

        public static IEndpointConventionBuilder MapSwaggerUI(this IEndpointRouteBuilder endpoints, string template)
        {
            var genOptions = endpoints.ServiceProvider
                .GetRequiredService<IOptions<SwaggerGenOptions>>();
            var dataSource = new SwaggerEndpointDataSource(genOptions, template);
            endpoints.DataSources.Add(dataSource);
            return dataSource;
        }

        public static IEndpointConventionBuilder MapSwaggerV2(this IEndpointRouteBuilder endpoints, string template)
        {
            return endpoints.MapSwagger(template, true);
        }

        public static IEndpointConventionBuilder MapSwaggerV3(this IEndpointRouteBuilder endpoints, string template)
        {
            return endpoints.MapSwagger(template, false);
        }
    }
}