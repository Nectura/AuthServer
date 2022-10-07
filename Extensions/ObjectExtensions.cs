using System.Reflection;
using Newtonsoft.Json;

namespace AuthServer.Extensions;

public static class ObjectExtensions
{
    public static string GetQueryParametersFromJsonProperties(this object model)
    {
        var queryParameters = new HashSet<string>();
        foreach (var property in model.GetType().GetProperties())
        {
            var jsonPropertyAttribute = property.GetCustomAttribute(typeof(JsonPropertyAttribute));
            if (jsonPropertyAttribute == default) continue;
            var attributeValue = (JsonPropertyAttribute)jsonPropertyAttribute;
            queryParameters.Add($"{(queryParameters.Count == 0 ? "?" : "&")}{attributeValue.PropertyName}={property.GetValue(model)}");
        }
        return string.Join(string.Empty, queryParameters);
    }
}