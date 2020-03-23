using JudgeWeb.Domains.Problems.Portion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace JudgeWeb.Domains.Problems
{
    public static class EntityTypeConfiguration
    {
        public static void AddProblemDomain<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.AddScoped<IExportProvider, KattisExportProvider>();
            services.AddScoped<KattisImportProvider>();
            services.AddScoped<XmlImportProvider>();

            IImportProvider.ImportServiceKinds = new Dictionary<string, Type>
            {
                ["kattis"] = typeof(KattisImportProvider),
                ["xysxml"] = typeof(XmlImportProvider),
            };

            services.AddScoped<IProblemViewProvider, MarkdownProblemViewProvider>();
            services.AddScoped<IProblemFacade, DbContextProblemFacade<TContext>>();
            services.AddScoped<IJudgementFacade, DbContextJudgementFacade<TContext>>();
        }
    }
}
