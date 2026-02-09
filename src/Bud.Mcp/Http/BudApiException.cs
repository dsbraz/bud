using System.Net;
using System.Text.Json;

namespace Bud.Mcp.Http;

public sealed class BudApiException(
    string message,
    HttpStatusCode statusCode,
    string? title = null,
    string? detail = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? validationErrors = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? Title { get; } = title;
    public string? Detail { get; } = detail;
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors { get; } =
        validationErrors ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

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
            var title = TryGetPropertyString(root, "title", out var titleValue) ? titleValue : null;
            var detail = TryGetPropertyString(root, "detail", out var detailValue) ? detailValue : null;
            var errors = ExtractValidationErrors(root);

            if (errors.Count > 0)
            {
                var message = BuildValidationMessage(errors);
                return new BudApiException(message, response.StatusCode, title, detail, errors);
            }

            if (!string.IsNullOrWhiteSpace(detail))
            {
                return new BudApiException(detail!, response.StatusCode, title, detail, errors);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                return new BudApiException(title!, response.StatusCode, title, detail, errors);
            }
        }
        catch (JsonException)
        {
            return new BudApiException(fallbackMessage, response.StatusCode);
        }

        return new BudApiException(fallbackMessage, response.StatusCode);
    }

    private static Dictionary<string, IReadOnlyList<string>> ExtractValidationErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }

        var validationErrors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var property in errors.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var messages = property.Value
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Cast<string>()
                .ToList();

            if (messages.Count > 0)
            {
                validationErrors[property.Name] = messages;
            }
        }

        return validationErrors;
    }

    private static string BuildValidationMessage(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        var entries = errors.Select(error => $"{error.Key}: {string.Join(" | ", error.Value)}");
        return $"Falha de validação: {string.Join("; ", entries)}";
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
