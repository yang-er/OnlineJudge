using Markdig;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JudgeWeb.Features
{
    public static class MarkdownExtensions
    {
        private static MarkdownPipeline PipelineFactory(IServiceProvider provider)
        {
            return new MarkdownPipelineBuilder()
                .Use<KatexExtension>()
                .Use<HeadingIdExtension>()
                .UseSoftlineBreakAsHardlineBreak()
                .UseNoFollowLinks()
                .UsePipeTables()
                .UseBootstrap()
                .Build();
        }

        public static IServiceCollection AddMarkdown(this IServiceCollection services)
        {
            services.AddSingleton<MarkdownPipeline>(PipelineFactory);
            services.AddSingleton<IMarkdownService, MarkdigService>();
            return services;
        }
    }
}
