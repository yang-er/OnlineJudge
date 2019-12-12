using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    public class ContentFileResult : FileResult
    {
        private string _fileName;

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with the provided <paramref name="fileName"/>
        /// and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public ContentFileResult(string fileName, string contentType)
            : this(fileName, MediaTypeHeaderValue.Parse(contentType))
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
        }

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public ContentFileResult(string fileName, MediaTypeHeaderValue contentType)
            : base(contentType?.ToString())
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set => _fileName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the <see cref="IFileProvider"/> used to resolve paths.
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ContentFileResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
