using System.Text.Json;
using System.Text.Json.Nodes;
using Bud.Mcp.Auth;
using Bud.Mcp.Configuration;
using Bud.Mcp.Protocol;
using Bud.Mcp.Tools.Generation;

var builder = WebApplication.CreateBuilder(args);

var options = BudMcpOptions.FromConfiguration(builder.Configuration);

var commandRunner = new ToolCatalogCommandRunner();
var commandResult = await commandRunner.TryExecuteAsync(args, options);
if (commandResult.Handled)
{
    Environment.ExitCode = commandResult.ExitCode;
    return;
}

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<IMcpSessionStore, InMemoryMcpSessionStore>();
builder.Services.AddSingleton<McpJsonRpcDispatcher>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ok" }));

app.MapPost("/", async (
    HttpContext httpContext,
    IMcpSessionStore sessionStore,
    McpJsonRpcDispatcher dispatcher,
    CancellationToken cancellationToken) =>
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
                    "Header Mcp-Session-Id é obrigatório para este método."));
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
        if (response is null)
        {
            return Results.NoContent();
        }

        return Results.Json(response);
    }
});

await app.RunAsync();

static string? ReadSessionIdHeader(IHeaderDictionary headers)
{
    if (!TryGetHeaderValue(headers, out var value, "MCP-Session-Id", "Mcp-Session-Id", "X-Mcp-Session-Id"))
    {
        return null;
    }

    var raw = value;
    if (string.IsNullOrWhiteSpace(raw))
    {
        return null;
    }

    if (!Guid.TryParse(raw, out var parsed))
    {
        throw new InvalidOperationException("Header Mcp-Session-Id deve ser um GUID válido.");
    }

    return parsed.ToString();
}

static void SetSessionHeaders(IHeaderDictionary headers, string sessionId)
{
    headers["MCP-Session-Id"] = sessionId;
    headers["X-Mcp-Session-Id"] = sessionId;
}

static bool TryGetHeaderValue(IHeaderDictionary headers, out string? value, params string[] headerNames)
{
    foreach (var name in headerNames)
    {
        if (headers.TryGetValue(name, out var headerValue))
        {
            var raw = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                value = raw;
                return true;
            }
        }
    }

    value = null;
    return false;
}

static JsonNode? TryGetId(JsonElement root)
{
    return root.TryGetProperty("id", out var idProperty)
        ? JsonNode.Parse(idProperty.GetRawText())
        : null;
}

public partial class Program;
