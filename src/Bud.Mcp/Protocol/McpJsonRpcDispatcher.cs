using System.Text.Json;
using System.Text.Json.Nodes;
using Bud.Mcp.Http;
using Bud.Mcp.Tools;

namespace Bud.Mcp.Protocol;

public sealed class McpJsonRpcDispatcher
{
    private const string ProtocolVersion = "2025-06-18";

    public async Task<JsonObject?> DispatchAsync(
        JsonElement root,
        McpToolService toolService,
        CancellationToken cancellationToken = default)
    {
        if (!root.TryGetProperty("method", out var methodProperty) || methodProperty.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Método JSON-RPC é obrigatório.");
        }

        var method = methodProperty.GetString()!;
        var hasId = root.TryGetProperty("id", out var idProperty);
        var idNode = hasId ? JsonNode.Parse(idProperty.GetRawText()) : null;

        if (string.Equals(method, "notifications/initialized", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var result = await HandleMethodAsync(method, root, toolService, cancellationToken);
            if (!hasId)
            {
                return null;
            }

            return new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = idNode!.DeepClone(),
                ["result"] = result
            };
        }
        catch (Exception ex)
        {
            if (!hasId)
            {
                return null;
            }

            return CreateErrorResponse(idNode!, ex.Message);
        }
    }

    public static JsonObject CreateErrorResponse(JsonNode idNode, string message, int code = -32000)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = idNode.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
    }

    private static async Task<JsonObject> HandleMethodAsync(
        string method,
        JsonElement root,
        McpToolService toolService,
        CancellationToken cancellationToken)
    {
        return method switch
        {
            "initialize" => InitializeResult(),
            "ping" => new JsonObject(),
            "prompts/list" => new JsonObject { ["prompts"] = new JsonArray() },
            "tools/list" => ToolsListResult(toolService),
            "tools/call" => await ToolsCallResultAsync(root, toolService, cancellationToken),
            _ => throw new InvalidOperationException($"Método JSON-RPC não suportado: {method}.")
        };
    }

    private static JsonObject InitializeResult()
    {
        return new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject(),
                ["prompts"] = new JsonObject()
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "bud-mcp",
                ["version"] = "1.0.0"
            }
        };
    }

    private static JsonObject ToolsListResult(McpToolService toolService)
    {
        var tools = new JsonArray();
        foreach (var tool in toolService.GetTools())
        {
            tools.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["inputSchema"] = tool.InputSchema.DeepClone()
            });
        }

        return new JsonObject { ["tools"] = tools };
    }

    private static async Task<JsonObject> ToolsCallResultAsync(
        JsonElement root,
        McpToolService toolService,
        CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("params", out var paramsNode))
        {
            throw new InvalidOperationException("Parâmetro params é obrigatório em tools/call.");
        }

        if (!paramsNode.TryGetProperty("name", out var nameNode) || nameNode.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Parâmetro name é obrigatório em tools/call.");
        }

        var toolName = nameNode.GetString()!;
        var arguments = paramsNode.TryGetProperty("arguments", out var argsNode)
            ? argsNode
            : default;

        try
        {
            var execution = await toolService.ExecuteAsync(toolName, arguments, cancellationToken);
            return ToolResult(execution, isError: false);
        }
        catch (Exception ex) when (ex is BudApiException or InvalidOperationException or TimeoutException or HttpRequestException)
        {
            var payload = new JsonObject
            {
                ["message"] = ex.Message,
                ["tool"] = toolName
            };

            if (ex is BudApiException budApiException)
            {
                payload["statusCode"] = (int)budApiException.StatusCode;
                if (!string.IsNullOrWhiteSpace(budApiException.Title))
                {
                    payload["title"] = budApiException.Title;
                }

                if (!string.IsNullOrWhiteSpace(budApiException.Detail))
                {
                    payload["detail"] = budApiException.Detail;
                }

                if (budApiException.ValidationErrors.Count > 0)
                {
                    payload["errors"] = ToJsonErrors(budApiException.ValidationErrors);
                }
            }

            return ToolResult(payload, isError: true);
        }
    }

    private static JsonObject ToolResult(JsonNode payload, bool isError)
    {
        return new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = payload.ToJsonString()
                }
            },
            ["isError"] = isError
        };
    }

    private static JsonObject ToJsonErrors(IReadOnlyDictionary<string, IReadOnlyList<string>> validationErrors)
    {
        var jsonErrors = new JsonObject();
        foreach (var (key, messages) in validationErrors)
        {
            var jsonMessages = new JsonArray();
            foreach (var message in messages)
            {
                jsonMessages.Add(message);
            }

            jsonErrors[key] = jsonMessages;
        }

        return jsonErrors;
    }
}
