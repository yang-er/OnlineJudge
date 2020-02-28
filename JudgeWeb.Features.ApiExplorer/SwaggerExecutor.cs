using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Features.ApiExplorer
{
    public class SwaggerExecutor
    {
        public OpenApiInfo Info { get; }
        private OpenApiDocument _document;
        private string _documentJson;
        private string _html;
        private readonly bool _asV2;

        public SwaggerExecutor(OpenApiInfo info, bool asV2)
        {
            Info = info;
            _asV2 = asV2;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var response = httpContext.Response;
            response.StatusCode = 200;
            response.ContentType = "application/json;charset=utf-8";

            if (_documentJson == null)
            {
                var swaggerGen = httpContext.RequestServices
                    .GetRequiredService<ISwaggerProvider>();
                _document = swaggerGen.GetSwagger(Info);

                using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
                var jsonWriter = new OpenApiJsonWriter(textWriter);
                if (_asV2) _document.SerializeAsV2(jsonWriter);
                else _document.SerializeAsV3(jsonWriter);

                _documentJson = textWriter.ToString();
            }

            await response.WriteAsync(_documentJson, new UTF8Encoding(false));
        }

        public async Task InvokeAsHtml(HttpContext httpContext)
        {
            var response = httpContext.Response;
            response.StatusCode = 200;
            response.ContentType = "text/html;charset=utf-8";

            if (_html == null)
            {
                var swaggerGen = httpContext.RequestServices
                    .GetRequiredService<ISwaggerProvider>();
                var hostEnv = httpContext.RequestServices
                    .GetRequiredService<IWebHostEnvironment>();
                _document = swaggerGen.GetSwagger(Info);

                var htmlTemplate = hostEnv.WebRootFileProvider.GetFileInfo("static/nelmioapidoc/index.html.src");
                if (!htmlTemplate.Exists) throw new InvalidDataException();
                using var textWriter = new StringWriter(CultureInfo.InvariantCulture);

                using (var templateReader = htmlTemplate.CreateReadStream())
                using (var textReader = new StreamReader(templateReader))
                {
                    while (true)
                    {
                        string htmlContent = await textReader.ReadLineAsync();
                        if (string.IsNullOrEmpty(htmlContent)) break;

                        int titleIndex = htmlContent.IndexOf("</title>");
                        if (titleIndex != -1)
                        {
                            textWriter.Write(htmlContent.Substring(0, titleIndex));
                            textWriter.Write(Info.Title);
                            textWriter.WriteLine(htmlContent.Substring(titleIndex));
                            continue;
                        }

                        titleIndex = htmlContent.IndexOf("{\"spec\":");
                        if (titleIndex != -1)
                        {
                            titleIndex += 8;
                            textWriter.Write(htmlContent.Substring(0, titleIndex));
                            var jsonWriter = new OpenApiJsonWriter(textWriter);
                            _document.SerializeAsV2(jsonWriter);
                            textWriter.WriteLine(htmlContent.Substring(titleIndex));
                            continue;
                        }

                        textWriter.WriteLine(htmlContent);
                    }
                }

                _html = textWriter.ToString();
            }

            await response.WriteAsync(_html, new UTF8Encoding(false));
        }
    }
}
