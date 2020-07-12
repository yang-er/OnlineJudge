using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace JudgeWeb.Domains.Problems
{
    public static class ServiceCollectionExtensions
    {
        public static void AddProblemDomain(this IServiceCollection services)
        {
            IImportProvider.ImportServiceKinds = new Dictionary<string, Type>
            {
                ["kattis"] = typeof(KattisImportProvider),
                ["xysxml"] = typeof(XmlImportProvider),
                ["hustoj"] = typeof(FpsImportProvider),
                ["cfplyg"] = typeof(CodeforcesImportProvider),
                ["data"] = typeof(DataImportProvider),
            };
            
            services.TryAddFrom(typeof(ServiceCollectionExtensions).Assembly);
        }
    }
}
