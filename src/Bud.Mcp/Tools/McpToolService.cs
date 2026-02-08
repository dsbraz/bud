using Bud.Mcp.Auth;
using Bud.Mcp.Http;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Bud.Mcp.Tools;

public sealed class McpToolService(BudApiClient budApiClient, BudApiSession session)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    private readonly BudApiClient _budApiClient = budApiClient;
    private readonly BudApiSession _session = session;

    public IReadOnlyList<McpToolDefinition> GetTools() =>
    [
        CreateTool("auth_login", "Autentica o usuário da sessão MCP com e-mail.", new JsonObject
        {
            ["type"] = "object",
            ["required"] = new JsonArray("email"),
            ["properties"] = new JsonObject
            {
                ["email"] = new JsonObject
                {
                    ["type"] = "string",
                    ["format"] = "email"
                }
            }
        }),
        CreateTool("auth_whoami", "Retorna o contexto autenticado da sessão MCP.", new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject()
        }),
        CreateTool("tenant_list_available", "Lista organizações disponíveis para o usuário autenticado.", new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject()
        }),
        CreateTool("tenant_set_current", "Define o tenant atual da sessão.", new JsonObject
        {
            ["type"] = "object",
            ["required"] = new JsonArray("tenantId"),
            ["properties"] = new JsonObject
            {
                ["tenantId"] = new JsonObject
                {
                    ["type"] = "string",
                    ["format"] = "uuid"
                }
            }
        }),
        CreateTool("mission_create", "Cria uma missão.", SchemaFor<CreateMissionRequest>()),
        CreateTool("mission_get", "Busca uma missão por ID.", IdSchema()),
        CreateTool("mission_list", "Lista missões com filtros.", new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["scopeType"] = EnumSchema<MissionScopeType>(),
                ["scopeId"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
                ["search"] = new JsonObject { ["type"] = "string" },
                ["page"] = new JsonObject { ["type"] = "integer", ["default"] = 1 },
                ["pageSize"] = new JsonObject { ["type"] = "integer", ["default"] = 10 }
            }
        }),
        CreateTool("mission_update", "Atualiza uma missão.", IdWithPayloadSchema<UpdateMissionRequest>()),
        CreateTool("mission_delete", "Remove uma missão.", IdSchema()),
        CreateTool("mission_metric_create", "Cria uma métrica de missão.", SchemaFor<CreateMissionMetricRequest>()),
        CreateTool("mission_metric_get", "Busca uma métrica por ID.", IdSchema()),
        CreateTool("mission_metric_list", "Lista métricas de missão.", new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["missionId"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
                ["search"] = new JsonObject { ["type"] = "string" },
                ["page"] = new JsonObject { ["type"] = "integer", ["default"] = 1 },
                ["pageSize"] = new JsonObject { ["type"] = "integer", ["default"] = 10 }
            }
        }),
        CreateTool("mission_metric_update", "Atualiza uma métrica de missão.", IdWithPayloadSchema<UpdateMissionMetricRequest>()),
        CreateTool("mission_metric_delete", "Remove uma métrica de missão.", IdSchema()),
        CreateTool("metric_checkin_create", "Cria um check-in de métrica.", SchemaFor<CreateMetricCheckinRequest>()),
        CreateTool("metric_checkin_get", "Busca um check-in por ID.", IdSchema()),
        CreateTool("metric_checkin_list", "Lista check-ins com filtros.", new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["missionMetricId"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
                ["missionId"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
                ["page"] = new JsonObject { ["type"] = "integer", ["default"] = 1 },
                ["pageSize"] = new JsonObject { ["type"] = "integer", ["default"] = 10 }
            }
        }),
        CreateTool("metric_checkin_update", "Atualiza um check-in.", IdWithPayloadSchema<UpdateMetricCheckinRequest>()),
        CreateTool("metric_checkin_delete", "Remove um check-in.", IdSchema())
    ];

    public async Task<JsonNode> ExecuteAsync(string name, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        return name switch
        {
            "auth_login" => await LoginAsync(arguments, cancellationToken),
            "auth_whoami" => WhoAmI(),
            "tenant_list_available" => await TenantListAvailableAsync(cancellationToken),
            "tenant_set_current" => await TenantSetCurrentAsync(arguments, cancellationToken),
            "mission_create" => Serialize(await _budApiClient.CreateMissionAsync(Deserialize<CreateMissionRequest>(arguments), cancellationToken)),
            "mission_get" => Serialize(await _budApiClient.GetMissionAsync(ParseId(arguments), cancellationToken)),
            "mission_list" => await MissionListAsync(arguments, cancellationToken),
            "mission_update" => await MissionUpdateAsync(arguments, cancellationToken),
            "mission_delete" => await MissionDeleteAsync(arguments, cancellationToken),
            "mission_metric_create" => Serialize(await _budApiClient.CreateMissionMetricAsync(Deserialize<CreateMissionMetricRequest>(arguments), cancellationToken)),
            "mission_metric_get" => Serialize(await _budApiClient.GetMissionMetricAsync(ParseId(arguments), cancellationToken)),
            "mission_metric_list" => await MissionMetricListAsync(arguments, cancellationToken),
            "mission_metric_update" => await MissionMetricUpdateAsync(arguments, cancellationToken),
            "mission_metric_delete" => await MissionMetricDeleteAsync(arguments, cancellationToken),
            "metric_checkin_create" => Serialize(await _budApiClient.CreateMetricCheckinAsync(Deserialize<CreateMetricCheckinRequest>(arguments), cancellationToken)),
            "metric_checkin_get" => Serialize(await _budApiClient.GetMetricCheckinAsync(ParseId(arguments), cancellationToken)),
            "metric_checkin_list" => await MetricCheckinListAsync(arguments, cancellationToken),
            "metric_checkin_update" => await MetricCheckinUpdateAsync(arguments, cancellationToken),
            "metric_checkin_delete" => await MetricCheckinDeleteAsync(arguments, cancellationToken),
            _ => throw new InvalidOperationException($"Tool '{name}' não é suportada.")
        };
    }

    private JsonNode WhoAmI()
    {
        var auth = _session.AuthContext ?? throw new InvalidOperationException("Sessão MCP não autenticada.");
        return new JsonObject
        {
            ["email"] = auth.Email,
            ["displayName"] = auth.DisplayName,
            ["isGlobalAdmin"] = auth.IsGlobalAdmin,
            ["collaboratorId"] = auth.CollaboratorId?.ToString(),
            ["organizationId"] = auth.OrganizationId?.ToString(),
            ["currentTenantId"] = _session.CurrentTenantId?.ToString()
        };
    }

    private async Task<JsonNode> LoginAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var email = TryGetString(arguments, "email");
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Parâmetro obrigatório ausente: email.");
        }

        await _session.LoginAsync(email, cancellationToken);
        return WhoAmI();
    }

    private async Task<JsonNode> TenantListAvailableAsync(CancellationToken cancellationToken)
    {
        var organizations = await _session.ListAvailableTenantsAsync(cancellationToken);
        return new JsonObject
        {
            ["items"] = Serialize(organizations),
            ["currentTenantId"] = _session.CurrentTenantId?.ToString()
        };
    }

    private async Task<JsonNode> TenantSetCurrentAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var tenantId = ParseGuid(arguments, "tenantId");
        await _session.SetCurrentTenantAsync(tenantId, cancellationToken);
        return new JsonObject
        {
            ["tenantId"] = tenantId.ToString(),
            ["message"] = "Tenant atualizado com sucesso."
        };
    }

    private async Task<JsonNode> MissionListAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var scopeType = TryParseEnum<MissionScopeType>(arguments, "scopeType");
        var scopeId = TryParseGuid(arguments, "scopeId");
        var search = TryGetString(arguments, "search");
        var page = TryGetInt(arguments, "page") ?? 1;
        var pageSize = TryGetInt(arguments, "pageSize") ?? 10;
        var result = await _budApiClient.ListMissionsAsync(scopeType, scopeId, search, page, pageSize, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> MissionUpdateAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var id = ParseGuid(arguments, "id");
        var payload = DeserializeFromProperty<UpdateMissionRequest>(arguments, "payload");
        var result = await _budApiClient.UpdateMissionAsync(id, payload, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> MissionDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        await _budApiClient.DeleteMissionAsync(ParseId(arguments), cancellationToken);
        return new JsonObject { ["deleted"] = true };
    }

    private async Task<JsonNode> MissionMetricListAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var missionId = TryParseGuid(arguments, "missionId");
        var search = TryGetString(arguments, "search");
        var page = TryGetInt(arguments, "page") ?? 1;
        var pageSize = TryGetInt(arguments, "pageSize") ?? 10;
        var result = await _budApiClient.ListMissionMetricsAsync(missionId, search, page, pageSize, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> MissionMetricUpdateAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var id = ParseGuid(arguments, "id");
        var payload = DeserializeFromProperty<UpdateMissionMetricRequest>(arguments, "payload");
        var result = await _budApiClient.UpdateMissionMetricAsync(id, payload, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> MissionMetricDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        await _budApiClient.DeleteMissionMetricAsync(ParseId(arguments), cancellationToken);
        return new JsonObject { ["deleted"] = true };
    }

    private async Task<JsonNode> MetricCheckinListAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var missionMetricId = TryParseGuid(arguments, "missionMetricId");
        var missionId = TryParseGuid(arguments, "missionId");
        var page = TryGetInt(arguments, "page") ?? 1;
        var pageSize = TryGetInt(arguments, "pageSize") ?? 10;
        var result = await _budApiClient.ListMetricCheckinsAsync(missionMetricId, missionId, page, pageSize, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> MetricCheckinUpdateAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var id = ParseGuid(arguments, "id");
        var payload = DeserializeFromProperty<UpdateMetricCheckinRequest>(arguments, "payload");
        var result = await _budApiClient.UpdateMetricCheckinAsync(id, payload, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> MetricCheckinDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        await _budApiClient.DeleteMetricCheckinAsync(ParseId(arguments), cancellationToken);
        return new JsonObject { ["deleted"] = true };
    }

    private static McpToolDefinition CreateTool(string name, string description, JsonObject schema) => new(name, description, schema);

    private static JsonObject IdSchema() =>
        new()
        {
            ["type"] = "object",
            ["required"] = new JsonArray("id"),
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" }
            }
        };

    private static JsonObject IdWithPayloadSchema<TPayload>() =>
        new()
        {
            ["type"] = "object",
            ["required"] = new JsonArray("id", "payload"),
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
                ["payload"] = SchemaFor<TPayload>()
            }
        };

    private static JsonObject SchemaFor<T>()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = true,
            ["description"] = $"Payload compatível com {typeof(T).Name}."
        };
    }

    private static JsonObject EnumSchema<TEnum>() where TEnum : struct, Enum
    {
        var values = Enum.GetNames<TEnum>();
        var jsonValues = new JsonArray();
        foreach (var value in values)
        {
            jsonValues.Add(value);
        }

        return new JsonObject
        {
            ["type"] = "string",
            ["enum"] = jsonValues
        };
    }

    private static Guid ParseId(JsonElement arguments) => ParseGuid(arguments, "id");

    private static Guid ParseGuid(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Parâmetro obrigatório ausente: {propertyName}.");
        }

        var value = property.GetString();
        if (!Guid.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"Parâmetro inválido: {propertyName} deve ser um GUID.");
        }

        return parsed;
    }

    private static Guid? TryParseGuid(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Guid.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"Parâmetro inválido: {propertyName} deve ser um GUID.");
        }

        return parsed;
    }

    private static T Deserialize<T>(JsonElement arguments)
    {
        var model = JsonSerializer.Deserialize<T>(arguments.GetRawText(), JsonOptions);
        return model ?? throw new InvalidOperationException("Payload inválido para a operação.");
    }

    private static T DeserializeFromProperty<T>(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var payload))
        {
            throw new InvalidOperationException($"Parâmetro obrigatório ausente: {propertyName}.");
        }

        var model = JsonSerializer.Deserialize<T>(payload.GetRawText(), JsonOptions);
        return model ?? throw new InvalidOperationException($"Payload inválido em {propertyName}.");
    }

    private static JsonNode Serialize<T>(T data)
    {
        return JsonNode.Parse(JsonSerializer.Serialize(data, JsonOptions))
            ?? throw new InvalidOperationException("Falha ao serializar retorno.");
    }

    private static string? TryGetString(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static int? TryGetInt(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static TEnum? TryParseEnum<TEnum>(JsonElement arguments, string propertyName) where TEnum : struct, Enum
    {
        if (!arguments.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var raw = property.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (Enum.TryParse<TEnum>(raw, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Valor inválido para {propertyName}: {raw}.");
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var intValue))
        {
            var enumValue = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
            if (Enum.IsDefined(enumValue))
            {
                return enumValue;
            }
        }

        throw new InvalidOperationException($"Valor inválido para {propertyName}.");
    }
}
