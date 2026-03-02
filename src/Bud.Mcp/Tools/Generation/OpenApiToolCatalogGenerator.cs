using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bud.Mcp.Tools.Generation;

public sealed class OpenApiToolCatalogGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private sealed record ToolOperationMap(string Name, string Description, string Method, string Path, ToolSchemaKind Kind);

    private enum ToolSchemaKind
    {
        Create,
        GetById,
        List,
        Update,
        Delete
    }

    private static readonly ToolOperationMap[] ToolMappings =
    [
        new("goal_create", "Cria uma meta.", "post", "/api/goals", ToolSchemaKind.Create),
        new("goal_get", "Busca uma meta por ID.", "get", "/api/goals/{id}", ToolSchemaKind.GetById),
        new("goal_list", "Lista metas com filtros.", "get", "/api/goals", ToolSchemaKind.List),
        new("goal_update", "Atualiza uma meta.", "patch", "/api/goals/{id}", ToolSchemaKind.Update),
        new("goal_delete", "Remove uma meta.", "delete", "/api/goals/{id}", ToolSchemaKind.Delete),

        new("goal_indicator_create", "Cria um indicador de meta.", "post", "/api/indicators", ToolSchemaKind.Create),
        new("goal_indicator_get", "Busca um indicador por ID.", "get", "/api/indicators/{id}", ToolSchemaKind.GetById),
        new("goal_indicator_list", "Lista indicadores de meta.", "get", "/api/indicators", ToolSchemaKind.List),
        new("goal_indicator_update", "Atualiza um indicador de meta.", "patch", "/api/indicators/{id}", ToolSchemaKind.Update),
        new("goal_indicator_delete", "Remove um indicador de meta.", "delete", "/api/indicators/{id}", ToolSchemaKind.Delete),

        new("indicator_checkin_create", "Cria um check-in de indicador.", "post", "/api/indicators/{indicatorId}/checkins", ToolSchemaKind.Create),
        new("indicator_checkin_get", "Busca um check-in por ID.", "get", "/api/indicators/{indicatorId}/checkins/{checkinId}", ToolSchemaKind.GetById),
        new("indicator_checkin_list", "Lista check-ins com filtros.", "get", "/api/indicators/{indicatorId}/checkins", ToolSchemaKind.List),
        new("indicator_checkin_update", "Atualiza um check-in.", "patch", "/api/indicators/{indicatorId}/checkins/{checkinId}", ToolSchemaKind.Update),
        new("indicator_checkin_delete", "Remove um check-in.", "delete", "/api/indicators/{indicatorId}/checkins/{checkinId}", ToolSchemaKind.Delete)
    ];

    public static JsonArray GenerateToolsFromOpenApi(string openApiJson)
    {
        var root = JsonNode.Parse(openApiJson)?.AsObject()
            ?? throw new InvalidOperationException("Documento OpenAPI inválido.");

        var tools = new JsonArray();
        foreach (var mapping in ToolMappings)
        {
            var operation = GetOperation(root, mapping.Path, mapping.Method);
            var inputSchema = BuildInputSchema(root, operation, mapping.Kind);

            tools.Add(new JsonObject
            {
                ["name"] = mapping.Name,
                ["description"] = mapping.Description,
                ["inputSchema"] = inputSchema
            });
        }

        return tools;
    }

    public static string BuildCatalogJson(string openApiJson)
    {
        var tools = GenerateToolsFromOpenApi(openApiJson);
        var catalog = new JsonObject
        {
            ["version"] = 1,
            ["tools"] = tools
        };

        return catalog.ToJsonString(JsonOptions);
    }

    private static JsonObject BuildInputSchema(JsonObject root, JsonObject operation, ToolSchemaKind kind)
    {
        return kind switch
        {
            ToolSchemaKind.Create => ResolveRequestBodySchema(root, operation),
            ToolSchemaKind.GetById or ToolSchemaKind.Delete => BuildIdSchema(root, operation),
            ToolSchemaKind.List => BuildQuerySchema(root, operation),
            ToolSchemaKind.Update => BuildUpdateSchema(root, operation),
            _ => throw new InvalidOperationException("Tipo de schema não suportado.")
        };
    }

    private static JsonObject BuildIdSchema(JsonObject root, JsonObject operation)
    {
        var pathParameters = GetParameters(root, operation)
            .Where(parameter => string.Equals(parameter["in"]?.GetValue<string>(), "path", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var parameter in pathParameters)
        {
            var parameterName = parameter["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                continue;
            }

            var schema = parameter["schema"] is JsonObject parameterSchema
                ? ResolveSchema(root, parameterSchema)
                : new JsonObject { ["type"] = "string", ["format"] = "uuid" };

            properties[parameterName] = schema;
            required.Add(parameterName);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = required,
            ["properties"] = properties
        };
    }

    private static JsonObject BuildQuerySchema(JsonObject root, JsonObject operation)
    {
        var parameters = GetParameters(root, operation);
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var parameter in parameters)
        {
            if (!string.Equals(parameter["in"]?.GetValue<string>(), "query", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parameterName = parameter["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(parameterName) || parameter["schema"] is not JsonObject parameterSchema)
            {
                continue;
            }

            properties[parameterName] = ResolveSchema(root, parameterSchema);

            var isRequired = parameter["required"]?.GetValue<bool>() == true;
            if (isRequired)
            {
                required.Add(parameterName);
            }
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = properties
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    private static JsonObject BuildUpdateSchema(JsonObject root, JsonObject operation)
    {
        var body = ResolveRequestBodySchema(root, operation);
        var schema = BuildIdSchema(root, operation);
        var properties = schema["properties"]!.AsObject();
        properties["payload"] = body;

        var required = schema["required"]!.AsArray();
        required.Add("payload");

        return schema;
    }

    private static JsonObject ResolveRequestBodySchema(JsonObject root, JsonObject operation)
    {
        if (operation["requestBody"] is not JsonObject requestBody)
        {
            throw new InvalidOperationException("OpenAPI sem requestBody para operação de escrita.");
        }

        var resolvedBody = ResolveRequestBody(root, requestBody);
        if (resolvedBody["content"] is not JsonObject content ||
            content["application/json"] is not JsonObject appJson ||
            appJson["schema"] is not JsonObject bodySchema)
        {
            throw new InvalidOperationException("OpenAPI sem schema application/json para requestBody.");
        }

        var resolved = ResolveSchema(root, bodySchema);
        resolved["additionalProperties"] ??= false;
        InferRequiredFromNonNullableProperties(resolved);
        return resolved;
    }

    private static void InferRequiredFromNonNullableProperties(JsonObject schema)
    {
        if (schema["required"] is not null || schema["properties"] is not JsonObject properties)
        {
            return;
        }

        var required = new JsonArray();
        foreach (var (name, value) in properties)
        {
            if (value is not JsonObject propertySchema || IsNullableProperty(propertySchema))
            {
                continue;
            }

            required.Add(name);
        }

        if (required.Count > 0)
        {
            schema["required"] = required;
        }
    }

    private static bool IsNullableProperty(JsonObject schema)
    {
        if (schema["type"] is JsonArray typeArray)
        {
            return typeArray.Any(t => string.Equals(t?.GetValue<string>(), "null", StringComparison.Ordinal));
        }

        if (schema["oneOf"] is JsonArray oneOf)
        {
            return oneOf.Any(v =>
                v is JsonObject obj &&
                string.Equals(obj["type"]?.GetValue<string>(), "null", StringComparison.Ordinal));
        }

        if (schema["anyOf"] is JsonArray anyOf)
        {
            return anyOf.Any(v =>
                v is JsonObject obj &&
                string.Equals(obj["type"]?.GetValue<string>(), "null", StringComparison.Ordinal));
        }

        return false;
    }

    private static JsonObject ResolveRequestBody(JsonObject root, JsonObject requestBody)
    {
        if (requestBody["$ref"] is not JsonValue refNode)
        {
            return requestBody;
        }

        var refPath = refNode.GetValue<string>();
        var resolved = ResolveReference(root, refPath)
            ?? throw new InvalidOperationException($"Referência OpenAPI não encontrada: {refPath}");

        return resolved.AsObject();
    }

    private static JsonObject ResolveSchema(JsonObject root, JsonObject schema)
    {
        if (schema["$ref"] is JsonValue refNode)
        {
            var refPath = refNode.GetValue<string>();
            var resolved = ResolveReference(root, refPath)
                ?? throw new InvalidOperationException($"Referência OpenAPI não encontrada: {refPath}");
            return ResolveSchema(root, resolved.AsObject());
        }

        if (schema["allOf"] is JsonArray allOf)
        {
            return MergeAllOf(root, allOf);
        }

        var clone = schema.DeepClone()?.AsObject() ?? new JsonObject();
        if (clone["properties"] is JsonObject properties)
        {
            var resolvedProperties = new JsonObject();
            foreach (var (propertyName, propertySchemaNode) in properties)
            {
                if (propertySchemaNode is JsonObject propertySchema)
                {
                    resolvedProperties[propertyName] = ResolveSchema(root, propertySchema);
                }
                else if (propertySchemaNode is not null)
                {
                    resolvedProperties[propertyName] = propertySchemaNode.DeepClone();
                }
            }

            clone["properties"] = resolvedProperties;
        }

        return clone;
    }

    private static JsonObject MergeAllOf(JsonObject root, JsonArray allOf)
    {
        var merged = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject()
        };

        var requiredSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in allOf)
        {
            if (item is not JsonObject itemObject)
            {
                continue;
            }

            var resolved = ResolveSchema(root, itemObject);
            if (resolved["properties"] is JsonObject properties)
            {
                var mergedProperties = merged["properties"]!.AsObject();
                foreach (var (key, value) in properties)
                {
                    mergedProperties[key] = value?.DeepClone();
                }
            }

            if (resolved["required"] is JsonArray required)
            {
                foreach (var requiredNode in required)
                {
                    var requiredProperty = requiredNode?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(requiredProperty))
                    {
                        requiredSet.Add(requiredProperty);
                    }
                }
            }
        }

        if (requiredSet.Count > 0)
        {
            merged["required"] = new JsonArray(requiredSet.Select(value => (JsonNode?)value).ToArray());
        }

        return merged;
    }

    private static List<JsonObject> GetParameters(JsonObject root, JsonObject operation)
    {
        var parameters = new List<JsonObject>();

        if (operation["parameters"] is JsonArray operationParameters)
        {
            parameters.AddRange(ResolveParameters(root, operationParameters));
        }

        return parameters;
    }

    private static IEnumerable<JsonObject> ResolveParameters(JsonObject root, JsonArray parameters)
    {
        foreach (var parameterNode in parameters)
        {
            if (parameterNode is not JsonObject parameterObject)
            {
                continue;
            }

            if (parameterObject["$ref"] is JsonValue refNode)
            {
                var refPath = refNode.GetValue<string>();
                var resolved = ResolveReference(root, refPath)
                    ?? throw new InvalidOperationException($"Referência OpenAPI não encontrada: {refPath}");
                yield return resolved.AsObject();
                continue;
            }

            yield return parameterObject;
        }
    }

    private static JsonObject GetOperation(JsonObject root, string path, string method)
    {
        if (root["paths"] is not JsonObject paths ||
            paths[path] is not JsonObject pathItem ||
            pathItem[method] is not JsonObject operation)
        {
            throw new InvalidOperationException($"Operação OpenAPI não encontrada: {method.ToUpperInvariant()} {path}");
        }

        return operation;
    }

    private static JsonNode? ResolveReference(JsonObject root, string refPath)
    {
        if (!refPath.StartsWith("#/", StringComparison.Ordinal))
        {
            return null;
        }

        var segments = refPath[2..].Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Replace("~1", "/").Replace("~0", "~"));

        JsonNode? current = root;
        foreach (var segment in segments)
        {
            if (current is JsonObject currentObject)
            {
                if (!currentObject.TryGetPropertyValue(segment, out current))
                {
                    return null;
                }

                continue;
            }

            return null;
        }

        return current;
    }
}
