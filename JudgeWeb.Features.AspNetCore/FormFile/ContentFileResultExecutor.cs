using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ContentFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<ContentFileResult>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        static readonly Action<ILogger, FileResult, string, string, Exception> _executingFileResult;
        static readonly Action<ILogger, Exception> _writingRangeToBody;

        static ContentFileResultExecutor()
        {
            _executingFileResult = LoggerMessage.Define<FileResult, string, string>(
                LogLevel.Information,
                new EventId(1, "ExecutingFileResult"),
                "Executing {FileResultType}, sending file '{FileDownloadPath}' with download name '{FileDownloadName}' ...");
            
            _writingRangeToBody = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(17, "WritingRangeToBody"),
                "Writing the requested range of bytes to the body...");
        }

        public ContentFileResultExecutor(ILogger<ContentFileResultExecutor> logger, IHostingEnvironment hostingEnvironment)
            : base(logger)
        {
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        /// <inheritdoc />
        public virtual Task ExecuteAsync(ActionContext context, ContentFileResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var fileInfo = GetFileInformation(result);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(
                    $"Invalid Path: {result.FileName}", result.FileName);
            }

            _executingFileResult(Logger, result, result.FileName, result.FileDownloadName, null);

            var lastModified = result.LastModified ?? fileInfo.LastModified;
            var (range, rangeLength, serveBody) = SetHeadersAndLog(
                context,
                result,
                fileInfo.Length,
                result.EnableRangeProcessing,
                lastModified,
                result.EntityTag);

            if (serveBody)
            {
                return WriteFileAsync(context, result, fileInfo, range, rangeLength);
            }

            return Task.CompletedTask;
        }

        protected virtual Task WriteFileAsync(ActionContext context, ContentFileResult result, IFileInfo fileInfo, RangeItemHeaderValue range, long rangeLength)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (range != null && rangeLength == 0)
            {
                return Task.CompletedTask;
            }

            var response = context.HttpContext.Response;
            var physicalPath = fileInfo.PhysicalPath;

            if (range != null)
            {
                _writingRangeToBody(Logger, null);
            }

            var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
            {
                if (range != null)
                {
                    return sendFile.SendFileAsync(
                        physicalPath,
                        offset: range.From ?? 0L,
                        count: rangeLength,
                        cancellation: default);
                }

                return sendFile.SendFileAsync(
                    physicalPath,
                    offset: 0,
                    count: null,
                    cancellation: default);
            }

            return WriteFileAsync(context.HttpContext, GetFileStream(fileInfo), range, rangeLength);
        }

        private IFileInfo GetFileInformation(ContentFileResult result)
        {
            var fileProvider = GetFileProvider(result);
            if (fileProvider is NullFileProvider)
            {
                throw new InvalidOperationException("No File Provider Configured");
            }

            var normalizedPath = result.FileName;
            if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.Substring(1);
            }

            var fileInfo = fileProvider.GetFileInfo(normalizedPath);
            return fileInfo;
        }

        private IFileProvider GetFileProvider(ContentFileResult result)
        {
            if (result.FileProvider != null)
            {
                return result.FileProvider;
            }

            result.FileProvider = _hostingEnvironment.ContentRootFileProvider;
            return result.FileProvider;
        }

        protected virtual Stream GetFileStream(IFileInfo fileInfo)
        {
            return fileInfo.CreateReadStream();
        }
    }
}
