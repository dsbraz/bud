using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bud.Mcp.Http;
using Bud.Mcp.Tools;

namespace Bud.Mcp.Protocol;

public sealed class StdioJsonRpcServer(McpToolService toolService)
{
    private const string ProtocolVersion = "2025-06-18";
    private readonly McpToolService _toolService = toolService;
    private bool _useContentLengthFraming = true;

    public async Task RunAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var readResult = await ReadMessageAsync(input, cancellationToken);
            if (readResult is null)
            {
                break;
            }

            _useContentLengthFraming = readResult.Value.UsesContentLengthFraming;
            using var document = JsonDocument.Parse(readResult.Value.Payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("method", out var methodProperty) || methodProperty.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var method = methodProperty.GetString();
            var hasId = root.TryGetProperty("id", out var idProperty);
            var idNode = hasId ? JsonNode.Parse(idProperty.GetRawText()) : null;

            if (string.Equals(method, "notifications/initialized", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var result = await HandleMethodAsync(method!, root, cancellationToken);
                if (hasId)
                {
                    await WriteSuccessResponseAsync(output, idNode!, result, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                if (!hasId)
                {
                    continue;
                }

                await WriteErrorResponseAsync(output, idNode!, ex, cancellationToken);
            }
        }
    }

    private async Task<JsonObject> HandleMethodAsync(string method, JsonElement root, CancellationToken cancellationToken)
    {
        return method switch
        {
            "initialize" => InitializeResult(),
            "ping" => new JsonObject(),
            "tools/list" => ToolsListResult(),
            "tools/call" => await ToolsCallResultAsync(root, cancellationToken),
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
                ["tools"] = new JsonObject()
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "bud-mcp",
                ["version"] = "1.0.0"
            }
        };
    }

    private JsonObject ToolsListResult()
    {
        var tools = new JsonArray();
        foreach (var tool in _toolService.GetTools())
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

    private async Task<JsonObject> ToolsCallResultAsync(JsonElement root, CancellationToken cancellationToken)
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
            var execution = await _toolService.ExecuteAsync(toolName, arguments, cancellationToken);
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

    private static async Task<ReadResult?> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var headerBuffer = new List<byte>(256);

        while (true)
        {
            var one = new byte[1];
            var read = await stream.ReadAsync(one, cancellationToken);
            if (read == 0)
            {
                if (headerBuffer.Count == 0)
                {
                    return null;
                }

                // Fallback para testes manuais via pipe (JSON-RPC sem framing Content-Length).
                var rawPayload = Encoding.UTF8.GetString([.. headerBuffer]).Trim();
                if (rawPayload.StartsWith('{') || rawPayload.StartsWith('['))
                {
                    return new ReadResult(Encoding.UTF8.GetBytes(rawPayload), UsesContentLengthFraming: false);
                }

                throw new InvalidOperationException("Fim de stream inesperado ao ler cabeçalho MCP.");
            }

            headerBuffer.Add(one[0]);

            // Framing MCP padrão (CRLF CRLF)
            if (HasCrLfHeaderTerminator(headerBuffer))
            {
                break;
            }

            // Compatibilidade com implementações que usam LF LF.
            if (HasLfHeaderTerminator(headerBuffer))
            {
                break;
            }

            // Fallback para JSON linha-a-linha sem Content-Length.
            if (headerBuffer.Count == 1 && (headerBuffer[0] == (byte)'{' || headerBuffer[0] == (byte)'['))
            {
                var rawLine = await ReadJsonLineAsync(stream, headerBuffer, cancellationToken);
                return new ReadResult(rawLine, UsesContentLengthFraming: false);
            }
        }

        var headerText = Encoding.ASCII.GetString([.. headerBuffer]);
        var contentLength = ParseContentLength(headerText);
        var body = new byte[contentLength];
        var offset = 0;
        while (offset < contentLength)
        {
            var read = await stream.ReadAsync(body.AsMemory(offset, contentLength - offset), cancellationToken);
            if (read == 0)
            {
                throw new InvalidOperationException("Fim de stream inesperado ao ler payload MCP.");
            }

            offset += read;
        }

        return new ReadResult(body, UsesContentLengthFraming: true);
    }

    private static bool HasCrLfHeaderTerminator(List<byte> buffer)
    {
        return buffer.Count >= 4 &&
               buffer[^4] == (byte)'\r' &&
               buffer[^3] == (byte)'\n' &&
               buffer[^2] == (byte)'\r' &&
               buffer[^1] == (byte)'\n';
    }

    private static bool HasLfHeaderTerminator(List<byte> buffer)
    {
        return buffer.Count >= 2 &&
               buffer[^2] == (byte)'\n' &&
               buffer[^1] == (byte)'\n';
    }

    private static async Task<byte[]> ReadJsonLineAsync(Stream stream, List<byte> firstBytes, CancellationToken cancellationToken)
    {
        var data = new List<byte>(firstBytes);
        while (true)
        {
            var one = new byte[1];
            var read = await stream.ReadAsync(one, cancellationToken);
            if (read == 0)
            {
                break;
            }

            data.Add(one[0]);
            if (one[0] == (byte)'\n')
            {
                break;
            }
        }

        var trimmed = Encoding.UTF8.GetString([.. data]).Trim();
        return Encoding.UTF8.GetBytes(trimmed);
    }

    private static int ParseContentLength(string headerText)
    {
        var lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = line["Content-Length:".Length..].Trim();
            if (int.TryParse(value, out var contentLength) && contentLength >= 0)
            {
                return contentLength;
            }
        }

        throw new InvalidOperationException("Cabeçalho Content-Length ausente ou inválido.");
    }

    private async Task WriteSuccessResponseAsync(Stream output, JsonNode id, JsonNode result, CancellationToken cancellationToken)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = result
        };

        await WriteMessageAsync(output, response, cancellationToken);
    }

    private async Task WriteErrorResponseAsync(Stream output, JsonNode id, Exception ex, CancellationToken cancellationToken)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new JsonObject
            {
                ["code"] = -32603,
                ["message"] = ex.Message
            }
        };

        await WriteMessageAsync(output, response, cancellationToken);
    }

    private async Task WriteMessageAsync(Stream output, JsonObject payload, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(payload.ToJsonString());
        if (_useContentLengthFraming)
        {
            var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
            await output.WriteAsync(header, cancellationToken);
            await output.WriteAsync(body, cancellationToken);
        }
        else
        {
            await output.WriteAsync(body, cancellationToken);
            await output.WriteAsync(Encoding.UTF8.GetBytes("\n"), cancellationToken);
        }
        await output.FlushAsync(cancellationToken);
    }

    private readonly record struct ReadResult(byte[] Payload, bool UsesContentLengthFraming);
}
