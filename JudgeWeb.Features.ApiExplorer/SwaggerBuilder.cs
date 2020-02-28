using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Features.ApiExplorer
{
    public class ApiExplorerBuilder
    {
        public IServiceCollection Services { get; }

        public ApiExplorerBuilder(IServiceCollection services, Action<SwaggerGenOptions> options)
        {
            Services = services;
            services.Replace(
                ServiceDescriptor.Singleton<
                    IApiDescriptionGroupCollectionProvider,
                    FilteredApiDescriptionGroupCollectionProvider>());
            services.AddSwaggerGen(options);
        }

        public ApiExplorerBuilder AddDocument(string title, string description, string version)
        {
            Services.Configure<SwaggerGenOptions>(options =>
                options.SwaggerDoc(
                    name: title.ToLower().Replace(' ', '_'),
                    info: new OpenApiInfo
                    {
                        Title = title,
                        Description = description,
                        Version = version,
                    }));

            return this;
        }

        public ApiExplorerBuilder AddSecurityScheme(string scheme, SecuritySchemeType type)
        {
            Services.Configure<SwaggerGenOptions>(options =>
            {
                options.AddSecurityDefinition(scheme, new OpenApiSecurityScheme
                {
                    Scheme = scheme,
                    Type = type
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference()
                            {
                                Id = scheme,
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return this;
        }

        public ApiExplorerBuilder IncludeXmlComments(string fileName)
        {
            if (System.IO.File.Exists(fileName))
                Services.Configure<SwaggerGenOptions>(options =>
                    options.IncludeXmlComments(fileName));
            return this;
        }

        public ApiExplorerBuilder IncludeXmlComments(IEnumerable<string> files)
        {
            foreach (var file in files)
                IncludeXmlComments(file);
            return this;
        }
    }
}
