using System.Net;
using System.Text.Json;

namespace Bud.Mcp.Http;

public sealed class BudApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public static async Task<BudApiException> FromHttpResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var fallbackMessage = $"Erro ao chamar a API Bud ({(int)response.StatusCode}).";

        if (string.IsNullOrWhiteSpace(body))
        {
            return new BudApiException(fallbackMessage, response.StatusCode);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (TryGetPropertyString(root, "detail", out var detail))
            {
                return new BudApiException(detail!, response.StatusCode);
            }

            if (TryGetPropertyString(root, "title", out var title))
            {
                return new BudApiException(title!, response.StatusCode);
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var firstError = property.Value.EnumerateArray().FirstOrDefault();
                    if (firstError.ValueKind == JsonValueKind.String)
                    {
                        return new BudApiException(firstError.GetString() ?? fallbackMessage, response.StatusCode);
                    }
                }
            }
        }
        catch (JsonException)
        {
            return new BudApiException(fallbackMessage, response.StatusCode);
        }

        return new BudApiException(fallbackMessage, response.StatusCode);
    }

    private static bool TryGetPropertyString(JsonElement root, string propertyName, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }
}
