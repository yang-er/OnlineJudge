using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Features.Storage
{
    public static class ProblemRepositoryExtensions
    {
        public static IServiceCollection AddProblemRepository(this IServiceCollection services)
        {
            services.AddSingleton<IFileRepository, LocalStorageRepository>();

            return services;
        }

        public static async Task<T> ReadAsync<T>(this IFileRepository io, string backstore, string fileName)
        {
            var tot = await io.ReadPartAsync(backstore, fileName);
            if (string.IsNullOrEmpty(tot)) return default(T);

            try
            {
                return tot.AsJson<T>();
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static Task WriteAsync<T>(this IFileRepository io, string backstore, string fileName, T obj)
        {
            try
            {
                var json = obj.ToJson();
                return io.WritePartAsync(backstore, fileName, json);
            }
            catch (JsonException ex)
            {
                return Task.FromException(ex);
            }
        }
    }
}
