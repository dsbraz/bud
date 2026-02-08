using Bud.Mcp.Auth;
using Bud.Mcp.Http;
using Bud.Mcp.Tools.Generation;
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

    private static readonly HashSet<string> DomainToolNames = new(StringComparer.Ordinal)
    {
        "mission_create",
        "mission_get",
        "mission_list",
        "mission_update",
        "mission_delete",
        "mission_metric_create",
        "mission_metric_get",
        "mission_metric_list",
        "mission_metric_update",
        "mission_metric_delete",
        "metric_checkin_create",
        "metric_checkin_get",
        "metric_checkin_list",
        "metric_checkin_update",
        "metric_checkin_delete"
    };

    private static readonly List<McpToolDefinition> ToolDefinitions =
    [
        CreateTool("auth_login", "Autentica o usuário da sessão MCP com e-mail.", new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
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
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject()
        }),
        CreateTool("tenant_list_available", "Lista organizações disponíveis para o usuário autenticado.", new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject()
        }),
        CreateTool("tenant_set_current", "Define o tenant atual da sessão.", new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
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
        CreateTool("session_bootstrap", "Retorna contexto de sessão e próximos passos recomendados para operar no Bud.", new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject()
        }),
        CreateTool("help_action_schema", "Retorna schema, campos obrigatórios e exemplo de payload para uma ação MCP.", new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["action"] = new JsonObject { ["type"] = "string" }
            }
        })
    ];

    private static readonly Dictionary<string, McpToolDefinition> ToolMap = new(StringComparer.Ordinal);

    static McpToolService()
    {
        for (var i = 0; i < ToolDefinitions.Count; i++)
        {
            ToolMap[ToolDefinitions[i].Name] = ToolDefinitions[i];
        }

        var generatedTools = McpToolCatalogStore.LoadToolsOrThrow();
        var requiredValidationErrors = ToolCatalogContractValidator.ValidateRequiredFields(generatedTools);
        if (requiredValidationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                "Catálogo MCP inválido para tools de domínio: " + string.Join(" ", requiredValidationErrors));
        }

        var generatedToolMap = generatedTools.ToDictionary(tool => tool.Name, tool => tool, StringComparer.Ordinal);

        foreach (var domainToolName in DomainToolNames)
        {
            if (!generatedToolMap.TryGetValue(domainToolName, out var generatedTool))
            {
                throw new InvalidOperationException(
                    $"Catálogo MCP não contém a tool de domínio obrigatória '{domainToolName}'.");
            }

            ToolDefinitions.Add(generatedTool);
            ToolMap[domainToolName] = generatedTool;
        }
    }

    private readonly BudApiClient _budApiClient = budApiClient;
    private readonly BudApiSession _session = session;
    private readonly IReadOnlyList<McpToolDefinition> _toolDefinitions = ToolDefinitions;

    public IReadOnlyList<McpToolDefinition> GetTools() => _toolDefinitions;

    public async Task<JsonNode> ExecuteAsync(string name, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        ValidateRequiredArguments(name, arguments);

        return name switch
        {
            "auth_login" => await LoginAsync(arguments, cancellationToken),
            "auth_whoami" => WhoAmI(),
            "tenant_list_available" => await TenantListAvailableAsync(cancellationToken),
            "tenant_set_current" => await TenantSetCurrentAsync(arguments, cancellationToken),
            "session_bootstrap" => await SessionBootstrapAsync(cancellationToken),
            "help_action_schema" => HelpActionSchema(arguments),
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

    private JsonObject WhoAmI()
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
        return BuildLoginBootstrap();
    }

    private JsonObject BuildLoginBootstrap()
    {
        return new JsonObject
        {
            ["whoami"] = WhoAmI(),
            ["requiresTenantForDomainTools"] = true,
            ["nextSteps"] = new JsonArray("tenant_list_available", "tenant_set_current", "help_action_schema", "session_bootstrap"),
            ["message"] = "Sessão autenticada. Selecione um tenant antes de executar ações de domínio."
        };
    }

    private async Task<JsonObject> TenantListAvailableAsync(CancellationToken cancellationToken)
    {
        var organizations = await _session.ListAvailableTenantsAsync(cancellationToken);
        return new JsonObject
        {
            ["items"] = Serialize(organizations),
            ["currentTenantId"] = _session.CurrentTenantId?.ToString()
        };
    }

    private async Task<JsonObject> TenantSetCurrentAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var tenantId = ParseGuid(arguments, "tenantId");
        await _session.SetCurrentTenantAsync(tenantId, cancellationToken);
        return new JsonObject
        {
            ["tenantId"] = tenantId.ToString(),
            ["message"] = "Tenant atualizado com sucesso."
        };
    }

    private async Task<JsonObject> SessionBootstrapAsync(CancellationToken cancellationToken)
    {
        var organizations = await _session.ListAvailableTenantsAsync(cancellationToken);

        return new JsonObject
        {
            ["whoami"] = WhoAmI(),
            ["currentTenantId"] = _session.CurrentTenantId?.ToString(),
            ["availableTenants"] = Serialize(organizations),
            ["requiresTenantForDomainTools"] = true,
            ["nextSteps"] = new JsonArray("tenant_set_current", "help_action_schema"),
            ["starterSchemas"] = new JsonArray
            {
                BuildActionHelp(ToolMap["mission_create"]),
                BuildActionHelp(ToolMap["mission_metric_create"]),
                BuildActionHelp(ToolMap["metric_checkin_create"])
            }
        };
    }

    private static JsonObject HelpActionSchema(JsonElement arguments)
    {
        var action = TryGetString(arguments, "action");
        if (string.IsNullOrWhiteSpace(action))
        {
            var actions = new JsonArray();
            foreach (var tool in ToolDefinitions)
            {
                actions.Add(BuildActionHelp(tool));
            }

            return new JsonObject { ["actions"] = actions };
        }

        if (!ToolMap.TryGetValue(action, out var toolDefinition))
        {
            throw new InvalidOperationException($"Ação não encontrada: {action}.");
        }

        return BuildActionHelp(toolDefinition);
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

    private async Task<JsonObject> MissionDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
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

    private async Task<JsonObject> MissionMetricDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
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

    private async Task<JsonObject> MetricCheckinDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        await _budApiClient.DeleteMetricCheckinAsync(ParseId(arguments), cancellationToken);
        return new JsonObject { ["deleted"] = true };
    }

    private static McpToolDefinition CreateTool(string name, string description, JsonObject schema) => new(name, description, schema);

    private static Guid ParseId(JsonElement arguments) => ParseGuid(arguments, "id");

    private static void ValidateRequiredArguments(string toolName, JsonElement arguments)
    {
        if (!ToolMap.TryGetValue(toolName, out var tool))
        {
            return;
        }

        ValidateRequiredAgainstSchema(tool.InputSchema, arguments, null);
    }

    private static void ValidateRequiredAgainstSchema(JsonObject schema, JsonElement payload, string? pathPrefix)
    {
        if (schema["required"] is JsonArray requiredProperties)
        {
            if (payload.ValueKind != JsonValueKind.Object)
            {
                var prefix = string.IsNullOrWhiteSpace(pathPrefix) ? string.Empty : $"{pathPrefix}.";
                throw new InvalidOperationException($"Payload inválido: objeto esperado em {prefix.TrimEnd('.')}.");
            }

            foreach (var requiredProperty in requiredProperties)
            {
                var requiredPropertyName = requiredProperty?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(requiredPropertyName))
                {
                    continue;
                }

                if (!payload.TryGetProperty(requiredPropertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                {
                    var fullPath = string.IsNullOrWhiteSpace(pathPrefix)
                        ? requiredPropertyName
                        : $"{pathPrefix}.{requiredPropertyName}";
                    throw new InvalidOperationException($"Parâmetro obrigatório ausente: {fullPath}.");
                }
            }
        }

        if (schema["properties"] is not JsonObject properties || payload.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var propertyEntry in properties)
        {
            if (propertyEntry.Value is not JsonObject propertySchema)
            {
                continue;
            }

            if (!payload.TryGetProperty(propertyEntry.Key, out var nestedProperty) || nestedProperty.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            var propertyType = propertySchema["type"]?.GetValue<string>();
            if (!string.Equals(propertyType, "object", StringComparison.Ordinal))
            {
                continue;
            }

            var childPath = string.IsNullOrWhiteSpace(pathPrefix)
                ? propertyEntry.Key
                : $"{pathPrefix}.{propertyEntry.Key}";
            ValidateRequiredAgainstSchema(propertySchema, nestedProperty, childPath);
        }
    }

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

    private static JsonObject BuildActionHelp(McpToolDefinition tool)
    {
        var required = new JsonArray();
        if (tool.InputSchema["required"] is JsonArray requiredFromSchema)
        {
            foreach (var item in requiredFromSchema)
            {
                required.Add(item?.GetValue<string>());
            }
        }

        return new JsonObject
        {
            ["name"] = tool.Name,
            ["description"] = tool.Description,
            ["required"] = required,
            ["inputSchema"] = tool.InputSchema.DeepClone(),
            ["example"] = BuildToolExample(tool.Name)
        };
    }

    private static JsonObject BuildToolExample(string toolName)
    {
        return toolName switch
        {
            "auth_login" => new JsonObject { ["email"] = "admin@getbud.co" },
            "auth_whoami" => new JsonObject(),
            "tenant_list_available" => new JsonObject(),
            "tenant_set_current" => new JsonObject { ["tenantId"] = "00000000-0000-0000-0000-000000000001" },
            "session_bootstrap" => new JsonObject(),
            "help_action_schema" => new JsonObject { ["action"] = "mission_create" },
            "mission_create" => new JsonObject
            {
                ["name"] = "teste do claude",
                ["description"] = "Missão criada via MCP",
                ["startDate"] = "2026-02-08T00:00:00Z",
                ["endDate"] = "2026-02-15T00:00:00Z",
                ["status"] = "Planned",
                ["scopeType"] = "Organization",
                ["scopeId"] = "00000000-0000-0000-0000-000000000001"
            },
            "mission_get" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000002" },
            "mission_list" => new JsonObject { ["scopeType"] = "Organization", ["scopeId"] = "00000000-0000-0000-0000-000000000001", ["page"] = 1, ["pageSize"] = 10 },
            "mission_update" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000002",
                ["payload"] = new JsonObject
                {
                    ["name"] = "Missão atualizada",
                    ["description"] = "Ajuste de escopo",
                    ["startDate"] = "2026-02-08T00:00:00Z",
                    ["endDate"] = "2026-02-20T00:00:00Z",
                    ["status"] = "Active",
                    ["scopeType"] = "Organization",
                    ["scopeId"] = "00000000-0000-0000-0000-000000000001"
                }
            },
            "mission_delete" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000002" },
            "mission_metric_create" => new JsonObject { ["missionId"] = "00000000-0000-0000-0000-000000000002", ["name"] = "NPS", ["type"] = "Quantitative", ["quantitativeType"] = "KeepAbove", ["minValue"] = 80, ["unit"] = "Percentage" },
            "mission_metric_get" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000003" },
            "mission_metric_list" => new JsonObject { ["missionId"] = "00000000-0000-0000-0000-000000000002", ["page"] = 1, ["pageSize"] = 10 },
            "mission_metric_update" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000003", ["payload"] = new JsonObject { ["name"] = "NPS trimestral", ["type"] = "Quantitative", ["quantitativeType"] = "KeepAbove", ["minValue"] = 85, ["unit"] = "Percentage" } },
            "mission_metric_delete" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000003" },
            "metric_checkin_create" => new JsonObject { ["missionMetricId"] = "00000000-0000-0000-0000-000000000003", ["value"] = 86.5, ["checkinDate"] = "2026-02-08T00:00:00Z", ["note"] = "Evolução semanal", ["confidenceLevel"] = 4 },
            "metric_checkin_get" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000004" },
            "metric_checkin_list" => new JsonObject { ["missionMetricId"] = "00000000-0000-0000-0000-000000000003", ["missionId"] = "00000000-0000-0000-0000-000000000002", ["page"] = 1, ["pageSize"] = 10 },
            "metric_checkin_update" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000004", ["payload"] = new JsonObject { ["value"] = 88.0, ["checkinDate"] = "2026-02-09T00:00:00Z", ["note"] = "Ajuste após revisão", ["confidenceLevel"] = 5 } },
            "metric_checkin_delete" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000004" },
            _ => new JsonObject()
        };
    }
}
