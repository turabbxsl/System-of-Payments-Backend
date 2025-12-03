using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PaymentsApi.Temporary
{
    public class IdempotencyHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            var method = context.ApiDescription.HttpMethod?.ToUpper();

            if (method == "POST" || method == "PUT" || method == "PATCH" || method == "DELETE")
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "Idempotency-Key",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Default = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                        Example = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString())
                    },
                    Description = "Unique idempotency key (GUID recommended)"
                });

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-User-Id",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Default = new Microsoft.OpenApi.Any.OpenApiString("11111111-1111-1111-1111-111111111111"),
                        Example = new Microsoft.OpenApi.Any.OpenApiString("11111111-1111-1111-1111-111111111111")
                    },
                    Description = "Unique UserId (GUID recommended)"
                });
            }
        }
    }
}
