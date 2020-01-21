﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApiDesignsServiceCollectionExtensions
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services,
            Action<SwaggerGenOptions> configOptions = null)
        {
            services.Replace(
                ServiceDescriptor.Singleton<
                    IApiDescriptionGroupCollectionProvider,
                    FilteredApiDescriptionGroupCollectionProvider>());

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("domjudge", new OpenApiInfo
                {
                    Title = "DOMjudge",
                    Description = "DOMjudge compact API v4",
                    Version = "7.0.2",
                });

                options.AddSecurityDefinition("BasicAuth", new OpenApiSecurityScheme
                {
                    Scheme = "basic",
                    Type = SecuritySchemeType.Http,
                });

                configOptions?.Invoke(options);
            });

            return services;
        }

        public static IApplicationBuilder UseApiExplorer(this IApplicationBuilder app)
        {
            app.UseSwagger(options => options.RouteTemplate = "/api/swagger/{documentName}.json");

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/api/swagger/domjudge.json", "domjudge");
                options.RoutePrefix = "api/doc";
            });

            return app;
        }
    }
}