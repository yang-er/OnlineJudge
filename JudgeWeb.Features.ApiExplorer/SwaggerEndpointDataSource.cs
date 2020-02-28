using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Features.ApiExplorer
{
    public class SwaggerEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private readonly IOptions<SwaggerGenOptions> _options;
        private readonly string _routeTemplate;
        private readonly bool _asV2;
        private readonly bool _asHtml;
        private List<Endpoint> _endpoints;
        private readonly List<Action<EndpointBuilder>> _conventions;

        public SwaggerEndpointDataSource(IOptions<SwaggerGenOptions> options, string route, bool? asV2 = null)
        {
            _options = options;
            _routeTemplate = route;
            if (asV2.HasValue)
                _asV2 = asV2.Value;
            else
                _asHtml = true;
            _conventions = new List<Action<EndpointBuilder>>();

            var getMeta = new HttpMethodMetadata(new[] { "GET" });
            _conventions.Add(builder => builder.Metadata.Add(getMeta));
        }

        private void Initialize()
        {
            int i = 0;
            _endpoints = new List<Endpoint>();
            foreach (var item in _options.Value.SwaggerGeneratorOptions.SwaggerDocs)
            {
                var executor = new SwaggerExecutor(item.Value, _asV2);

                var builder = new RouteEndpointBuilder(
                    requestDelegate: _asHtml ? (RequestDelegate)executor.InvokeAsHtml : executor.Invoke,
                    routePattern: RoutePatternFactory.Parse(_routeTemplate.Replace("{documentName}", item.Key)),
                    order: ++i);
                builder.DisplayName = $"Swagger.Document.{item.Key} ({item.Value.Title})";

                foreach (var convention in _conventions)
                    convention.Invoke(builder);

                _endpoints.Add(builder.Build());
            }
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (_endpoints == null) Initialize();
                return _endpoints;
            }
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            _conventions.Add(convention);
        }
    }
}
