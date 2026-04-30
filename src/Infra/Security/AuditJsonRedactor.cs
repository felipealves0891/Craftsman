using System.Text.Json;
using System.Text.Json.Nodes;

namespace Craftsman.Infra.Security;

public static class AuditJsonRedactor
{
    private static readonly string[] SensitiveFragments =
    [
        "password",
        "passwordhash",
        "securitystamp",
        "concurrencystamp",
        "token",
        "refreshtoken",
        "credential",
        "partnerkey",
        "clientsecret",
        "apiaccesscode",
        "accesscode"
    ];

    public static string? SerializeRedacted(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var node = JsonSerializer.SerializeToNode(value);
        Redact(node);
        return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static bool IsSensitive(string propertyName)
        => SensitiveFragments.Any(fragment => propertyName.Replace("_", string.Empty, StringComparison.Ordinal).Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    if (IsSensitive(property.Key))
                    {
                        jsonObject[property.Key] = "***REDACTED***";
                    }
                    else
                    {
                        Redact(property.Value);
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    Redact(item);
                }

                break;
        }
    }
}
