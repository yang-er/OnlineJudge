using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace JudgeWeb.Features.OjUpdate
{
    public static class OjExtensions
    {
        private static bool TryGetSection(this IConfiguration configuration,
            string name, out IConfigurationSection section)
        {
            section = configuration.GetSection(name);
            return section.GetChildren().Count() > 0;
        }

        public static IServiceCollection AddOjUpdateService(
            this IServiceCollection services,
            Action<IConfigurationBuilder> confOptions)
        {
            var confBuilder = new ConfigurationBuilder();
            confOptions.Invoke(confBuilder);
            IConfiguration configuration = confBuilder.Build();

            if (configuration.TryGetSection("hdu", out var hdu))
            {
                services.AddOptions<HdojUpdateOptions>().Bind(hdu);
                services.AddHostedService<HdojUpdateService>();
            }

            if (configuration.TryGetSection("cf", out var cf))
            {
                services.AddOptions<CfUpdateOptions>().Bind(cf);
                services.AddHostedService<CfUpdateService>();
            }

            if (configuration.TryGetSection("poj", out var poj))
            {
                services.AddOptions<PojUpdateOptions>().Bind(poj);
                services.AddHostedService<PojUpdateService>();
            }

            if (configuration.TryGetSection("vj", out var vj))
            {
                services.AddOptions<VjUpdateOptions>().Bind(vj);
                services.AddHostedService<VjUpdateService>();
            }

            return services;
        }
    }
}
