using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace AgentShop.Api.OpenApi;

/// <summary>
/// Slices nest their DTOs (PlaceOrder.Request, GetOrder.Response, ...). Without this, every nested
/// "Request"/"Response" would compete for the same schema name in the OpenAPI document.
/// Nested types are named after their declaring types: PlaceOrderRequest, ListProductsResponseItem.
/// </summary>
internal static class OpenApiSchemaNames
{
    public static string? Create(JsonTypeInfo typeInfo)
    {
        var type = typeInfo.Type;
        if (!type.IsNested || type.IsGenericType)
        {
            return OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
        }

        var name = type.Name;
        for (var declaring = type.DeclaringType; declaring is not null; declaring = declaring.DeclaringType)
        {
            name = declaring.Name + name;
        }

        return name;
    }
}
