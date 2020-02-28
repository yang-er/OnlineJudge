﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public interface ISwaggerProvider
    {
        OpenApiDocument GetSwagger(
            OpenApiInfo info,
            string host = null,
            string basePath = null);
    }

    public class UnknownSwaggerDocument : InvalidOperationException
    {
        public UnknownSwaggerDocument(string documentName, IEnumerable<string> knownDocuments)
            : base(string.Format("Unknown Swagger document - \"{0}\". Known Swagger documents: {1}",
                documentName,
                string.Join(",", knownDocuments?.Select(x => $"\"{x}\""))))
        {}
    }
}