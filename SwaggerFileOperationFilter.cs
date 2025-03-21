using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace PayMeChat_V_1
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileUploadParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) || 
                            p.ParameterType == typeof(IFormFileCollection) ||
                            (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(List<>) && 
                             p.ParameterType.GetGenericArguments()[0] == typeof(IFormFile)))
                .ToList();

            if (fileUploadParameters.Count == 0)
                return;

            // Add multipart/form-data content type
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>()
                        }
                    }
                }
            };

            var schema = operation.RequestBody.Content["multipart/form-data"].Schema;

            // Add file parameters
            foreach (var parameter in context.MethodInfo.GetParameters())
            {
                var name = parameter.Name;
                if (parameter.ParameterType == typeof(IFormFile))
                {
                    schema.Properties.Add(name, new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    });
                }
                else if (parameter.ParameterType == typeof(int) || 
                         parameter.ParameterType == typeof(long) || 
                         parameter.ParameterType == typeof(double))
                {
                    schema.Properties.Add(name, new OpenApiSchema
                    {
                        Type = "integer"
                    });
                }
                else if (parameter.ParameterType == typeof(string))
                {
                    schema.Properties.Add(name, new OpenApiSchema
                    {
                        Type = "string"
                    });
                }
                // Add other parameter types as needed
            }
        }
    }
}
