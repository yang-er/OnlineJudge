﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public class SwaggerGeneratorOptions
    {
        public SwaggerGeneratorOptions()
        {
            SwaggerDocs = new Dictionary<string, OpenApiInfo>();
            DocInclusionPredicate = DefaultDocInclusionPredicate;
            OperationIdSelector = DefaultOperationIdSelector;
            TagsSelector = DefaultTagsSelector;
            SortKeySelector = DefaultSortKeySelector;
            SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>();
            SecurityRequirements = new List<OpenApiSecurityRequirement>();
            ParameterFilters = new List<IParameterFilter>();
            OperationFilters = new List<IOperationFilter>();
            DocumentFilters = new List<IDocumentFilter>();
        }

        public IDictionary<string, OpenApiInfo> SwaggerDocs { get; set; }

        public Func<OpenApiInfo, ApiDescription, bool> DocInclusionPredicate { get; set; }

        public bool IgnoreObsoleteActions { get; set; }

        public Func<IEnumerable<ApiDescription>, ApiDescription> ConflictingActionsResolver { get; set; }

        public Func<ApiDescription, string> OperationIdSelector { get; set; }

        public Func<ApiDescription, IList<string>> TagsSelector { get; set; }

        public Func<ApiDescription, string> SortKeySelector { get; set; }

        public bool DescribeAllParametersInCamelCase { get; set; }

        public IDictionary<string, OpenApiSecurityScheme> SecuritySchemes { get; set; }

        public IList<OpenApiSecurityRequirement> SecurityRequirements { get; set; }

        public IList<IParameterFilter> ParameterFilters { get; set; }

        public List<IOperationFilter> OperationFilters { get; set; }

        public IList<IDocumentFilter> DocumentFilters { get; set; }

        private bool DefaultDocInclusionPredicate(OpenApiInfo info, ApiDescription apiDescription)
        {
            return apiDescription.GroupName == null || apiDescription.GroupName == info.Title;
        }

        private string DefaultOperationIdSelector(ApiDescription apiDescription)
        {
            return apiDescription.ActionDescriptor.AttributeRouteInfo?.Name;
        }

        private IList<string> DefaultTagsSelector(ApiDescription apiDescription)
        {
            return new[] { apiDescription.ActionDescriptor.RouteValues["controller"] };
        }

        private string DefaultSortKeySelector(ApiDescription apiDescription)
        {
            return TagsSelector(apiDescription).First();
        }
    }
}