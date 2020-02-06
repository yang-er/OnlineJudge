using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features
{
    public static class MarkdownExtensions
    {
        public static void TryPrepend<TRenderer>(this ObjectRendererCollection list)
            where TRenderer : class, IMarkdownObjectRenderer, new()
        {
            if (!list.Contains<TRenderer>())
            {
                list.Insert(0, new TRenderer());
            }
        }

        public static void Transverse<T>(this IMarkdownObject obj, Action<T> action) where T : Inline
        {
            if (obj is T lft)
                action(lft);
            else if (obj is ContainerBlock cblk)
                foreach (var item in cblk)
                    item?.Transverse(action);
            else if (obj is LeafBlock lblk)
                lblk.Inline?.Transverse(action);
            else if (obj is ContainerInline ctk)
                foreach (var item in ctk)
                    item?.Transverse(action);
        }

        public static string Render(this IMarkdownService markdown, string source)
        {
            return markdown.RenderAsHtml(markdown.Parse(source));
        }

        public static async Task TransverseAsync<T>(this IMarkdownObject obj, Func<T, Task> action) where T : Inline
        {
            if (obj is T lft)
                await action(lft);
            else if (obj is ContainerBlock cblk)
                foreach (var item in cblk)
                    await item.TransverseAsync(action);
            else if (obj is LeafBlock lblk && lblk.Inline != null)
                await lblk.Inline.TransverseAsync(action);
            else if (obj is ContainerInline ctk)
                foreach (var item in ctk)
                    await item.TransverseAsync(action);
        }

        public static IServiceCollection AddMarkdown(this IServiceCollection services)
        {
            services.AddScoped<IMarkdownService, MarkdigService>();
            return services;
        }
    }
}
