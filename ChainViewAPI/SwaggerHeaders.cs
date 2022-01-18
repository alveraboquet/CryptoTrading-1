using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
namespace ChainViewAPI
{
    public class AddRequiredHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            var accountId = new OpenApiParameter
            {
                Name = "account-id",
                In = ParameterLocation.Header,
                Style = ParameterStyle.Form,
                AllowEmptyValue = true,
                Required = false,
                Example = new OpenApiString("2"),
            };
            operation.Parameters.Add(accountId);

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "account-token",
                In = ParameterLocation.Header,
                Style = ParameterStyle.Form,
                AllowEmptyValue = true,
                Required = false,
                Example = new OpenApiString("c5791036-b94b-4c6a-bc5e-49c0f31397sd")
            });
        }
    }
}