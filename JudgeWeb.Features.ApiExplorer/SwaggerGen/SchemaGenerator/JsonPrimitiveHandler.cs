using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public class JsonPrimitiveHandler : SchemaGeneratorHandler
    {
        public override bool CanCreateSchemaFor(Type type, out bool shouldBeReferenced)
        {
            if (PrimitiveTypeMap.ContainsKey(type))
            {
                shouldBeReferenced = false;
                return true;
            }

            shouldBeReferenced = false; return false;
        }

        public override OpenApiSchema CreateSchema(Type type, SchemaRepository schemaRepository)
        {
            return PrimitiveTypeMap[type]();
        }

        private static readonly Dictionary<Type, Func<OpenApiSchema>> PrimitiveTypeMap = new Dictionary<Type, Func<OpenApiSchema>>
        {
            [ typeof(bool) ] = () => new OpenApiSchema { Type = "boolean" },
            [ typeof(byte) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(sbyte) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(short) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(ushort) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(int) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(uint) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(long) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(ulong) ] = () => new OpenApiSchema { Type = "integer" },
            [ typeof(float) ] = () => new OpenApiSchema { Type = "number" },
            [ typeof(double) ] = () => new OpenApiSchema { Type = "number" },
            [ typeof(decimal) ] = () => new OpenApiSchema { Type = "number" },
            [ typeof(byte[]) ] = () => new OpenApiSchema { Type = "string", Format = "byte" },
            [ typeof(string) ] = () => new OpenApiSchema { Type = "string" },
            [ typeof(char) ] = () => new OpenApiSchema { Type = "string" },
            [ typeof(DateTime) ] = () => new OpenApiSchema { Type = "string", Format = "date-time" },
            [ typeof(DateTimeOffset) ] = () => new OpenApiSchema { Type = "string", Format = "date-time" },
            [ typeof(Guid) ] = () => new OpenApiSchema { Type = "string", Format = "uuid" },
            [ typeof(Uri) ] = () => new OpenApiSchema { Type = "string", Format = "uri" },
            [ typeof(TimeSpan) ] = () => new OpenApiSchema { Type = "string", Format = "rel-time" }
        };
    }
}