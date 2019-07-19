using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JudgeWeb.Features.OjUpdate
{
    public static class OjExtensions
    {
        public static IServiceCollection AddOjUpdateService(
            this IServiceCollection services,
            Action<IConfigurationBuilder> confOptions)
        {
            var confBuilder = new ConfigurationBuilder();
            confOptions.Invoke(confBuilder);
            IConfiguration configuration = confBuilder.Build();

            services.AddOptions<HdojUpdateOptions>()
                    .Bind(configuration.GetSection("hdu"));
            services.AddHostedService<HdojUpdateService>();

            services.AddOptions<CfUpdateOptions>()
                    .Bind(configuration.GetSection("cf"));
            services.AddHostedService<CfUpdateService>();

            services.AddOptions<PojUpdateOptions>()
                    .Bind(configuration.GetSection("poj"));
            services.AddHostedService<PojUpdateService>();

            services.AddOptions<VjUpdateOptions>()
                    .Bind(configuration.GetSection("vj"));
            services.AddHostedService<VjUpdateService>();

            return services;
        }
    }
}
