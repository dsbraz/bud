using System.Text.Json;
using System.Text.Json.Nodes;
using Bud.Mcp.Auth;

namespace Bud.Mcp.Protocol;

public sealed class McpRequestProcessor(
    IMcpSessionStore sessionStore,
    McpJsonRpcDispatcher dispatcher) : IMcpRequestProcessor
{
    public async Task<IResult> ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { message = "Payload JSON inválido." });
        }

        using (document)
        {
            var root = document.RootElement;
            var idNode = TryGetId(root);
            var idOrNull = idNode ?? JsonValue.Create("null");

            if (!root.TryGetProperty("method", out var methodProperty) || methodProperty.ValueKind != JsonValueKind.String)
            {
                return Results.Json(McpJsonRpcDispatcher.CreateErrorResponse(idOrNull, "Método JSON-RPC é obrigatório."));
            }

            var method = methodProperty.GetString()!;
            McpSessionContext? sessionContext;
            string? requestedSessionId;

            try
            {
                requestedSessionId = ReadSessionIdHeader(httpContext.Request.Headers);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(McpJsonRpcDispatcher.CreateErrorResponse(idOrNull, ex.Message));
            }

            if (string.Equals(method, "initialize", StringComparison.Ordinal))
            {
                var session = await sessionStore.GetOrCreateAsync(requestedSessionId, cancellationToken);
                sessionContext = session.Context;
                SetSessionHeaders(httpContext.Response.Headers, sessionContext.SessionId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(requestedSessionId))
                {
                    return Results.Json(McpJsonRpcDispatcher.CreateErrorResponse(
                        idOrNull,
                        "Header MCP-SessionResponse-Id é obrigatório para este método."));
                }

                sessionContext = await sessionStore.GetExistingAsync(requestedSessionId, cancellationToken);
                if (sessionContext is null)
                {
                    return Results.Json(McpJsonRpcDispatcher.CreateErrorResponse(
                        idOrNull,
                        "Sessão MCP não encontrada ou expirada. Execute initialize novamente."));
                }

                SetSessionHeaders(httpContext.Response.Headers, sessionContext.SessionId);
            }

            var response = await dispatcher.DispatchAsync(root, sessionContext.ToolService, cancellationToken);
            return response is null ? Results.NoContent() : Results.Json(response);
        }
    }

    private static string? ReadSessionIdHeader(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("MCP-SessionResponse-Id", out var value))
        {
            return null;
        }

        var raw = value.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!Guid.TryParse(raw, out var parsed))
        {
            throw new InvalidOperationException("Header MCP-SessionResponse-Id deve ser um GUID válido.");
        }

        return parsed.ToString();
    }

    private static void SetSessionHeaders(IHeaderDictionary headers, string sessionId)
    {
        headers["MCP-SessionResponse-Id"] = sessionId;
    }

    private static JsonNode? TryGetId(JsonElement root)
    {
        return root.TryGetProperty("id", out var idProperty)
            ? JsonNode.Parse(idProperty.GetRawText())
            : null;
    }
}
