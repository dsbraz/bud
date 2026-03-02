using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Globalization;
using Bud.Mcp.Auth;
using Bud.Mcp.Http;
using Bud.Mcp.Tools.Generation;
using Bud.Shared.Contracts;

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
        "goal_create",
        "goal_get",
        "goal_list",
        "goal_update",
        "goal_delete",
        "goal_indicator_create",
        "goal_indicator_get",
        "goal_indicator_list",
        "goal_indicator_update",
        "goal_indicator_delete",
        "indicator_checkin_create",
        "indicator_checkin_get",
        "indicator_checkin_list",
        "indicator_checkin_update",
        "indicator_checkin_delete"
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
        CreateTool("help_list_actions", "Lista ações MCP disponíveis com uma breve descrição.", new JsonObject
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
            "help_list_actions" => HelpListActions(),
            "help_action_schema" => HelpActionSchema(arguments),
            "goal_create" => Serialize(await _budApiClient.CreateGoalAsync(Deserialize<CreateGoalRequest>(arguments), cancellationToken)),
            "goal_get" => Serialize(await _budApiClient.GetGoalAsync(ParseId(arguments), cancellationToken)),
            "goal_list" => await GoalListAsync(arguments, cancellationToken),
            "goal_update" => await GoalUpdateAsync(arguments, cancellationToken),
            "goal_delete" => await GoalDeleteAsync(arguments, cancellationToken),
            "goal_indicator_create" => Serialize(await _budApiClient.CreateGoalIndicatorAsync(Deserialize<CreateIndicatorRequest>(arguments), cancellationToken)),
            "goal_indicator_get" => Serialize(await _budApiClient.GetGoalIndicatorAsync(ParseId(arguments), cancellationToken)),
            "goal_indicator_list" => await GoalIndicatorListAsync(arguments, cancellationToken),
            "goal_indicator_update" => await GoalIndicatorUpdateAsync(arguments, cancellationToken),
            "goal_indicator_delete" => await GoalIndicatorDeleteAsync(arguments, cancellationToken),
            "indicator_checkin_create" => Serialize(await _budApiClient.CreateIndicatorCheckinAsync(
                ParseGuid(arguments, "indicatorId"),
                Deserialize<CreateCheckinRequest>(arguments),
                cancellationToken)),
            "indicator_checkin_get" => Serialize(await _budApiClient.GetIndicatorCheckinAsync(
                ParseGuid(arguments, "indicatorId"),
                ParseGuid(arguments, "checkinId"),
                cancellationToken)),
            "indicator_checkin_list" => await IndicatorCheckinListAsync(arguments, cancellationToken),
            "indicator_checkin_update" => await IndicatorCheckinUpdateAsync(arguments, cancellationToken),
            "indicator_checkin_delete" => await IndicatorCheckinDeleteAsync(arguments, cancellationToken),
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
            ["nextSteps"] = new JsonArray("tenant_list_available", "tenant_set_current", "help_list_actions", "help_action_schema", "session_bootstrap"),
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
            ["nextSteps"] = new JsonArray("tenant_set_current", "help_list_actions", "help_action_schema"),
            ["starterSchemas"] = new JsonArray
            {
                BuildActionHelp(ToolMap["goal_create"]),
                BuildActionHelp(ToolMap["goal_indicator_create"]),
                BuildActionHelp(ToolMap["indicator_checkin_create"])
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

    private static JsonObject HelpListActions()
    {
        var actions = new JsonArray();
        foreach (var tool in ToolDefinitions)
        {
            actions.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description
            });
        }

        return new JsonObject { ["actions"] = actions };
    }

    private async Task<JsonNode> GoalListAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var scopeType = TryParseEnum<GoalScopeType>(arguments, "scopeType");
        var scopeId = TryParseGuid(arguments, "scopeId");
        var search = TryGetString(arguments, "search");
        var page = TryGetInt(arguments, "page") ?? 1;
        var pageSize = TryGetInt(arguments, "pageSize") ?? 10;
        var result = await _budApiClient.ListGoalsAsync(scopeType, scopeId, search, page, pageSize, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> GoalUpdateAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var id = ParseGuid(arguments, "id");
        var payload = DeserializeFromProperty<PatchGoalRequest>(arguments, "payload");
        var result = await _budApiClient.UpdateGoalAsync(id, payload, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonObject> GoalDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        await _budApiClient.DeleteGoalAsync(ParseId(arguments), cancellationToken);
        return new JsonObject { ["deleted"] = true };
    }

    private async Task<JsonNode> GoalIndicatorListAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var goalId = TryParseGuid(arguments, "goalId");
        var search = TryGetString(arguments, "search");
        var page = TryGetInt(arguments, "page") ?? 1;
        var pageSize = TryGetInt(arguments, "pageSize") ?? 10;
        var result = await _budApiClient.ListGoalIndicatorsAsync(goalId, search, page, pageSize, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> GoalIndicatorUpdateAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var id = ParseGuid(arguments, "id");
        var payload = DeserializeFromProperty<PatchIndicatorRequest>(arguments, "payload");
        var result = await _budApiClient.UpdateGoalIndicatorAsync(id, payload, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonObject> GoalIndicatorDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        await _budApiClient.DeleteGoalIndicatorAsync(ParseId(arguments), cancellationToken);
        return new JsonObject { ["deleted"] = true };
    }

    private async Task<JsonNode> IndicatorCheckinListAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var indicatorId = ParseGuid(arguments, "indicatorId");
        var page = TryGetInt(arguments, "page") ?? 1;
        var pageSize = TryGetInt(arguments, "pageSize") ?? 10;
        var result = await _budApiClient.ListIndicatorCheckinsAsync(indicatorId, page, pageSize, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonNode> IndicatorCheckinUpdateAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var indicatorId = ParseGuid(arguments, "indicatorId");
        var checkinId = ParseGuid(arguments, "checkinId");
        var payload = DeserializeFromProperty<PatchCheckinRequest>(arguments, "payload");
        var result = await _budApiClient.UpdateIndicatorCheckinAsync(indicatorId, checkinId, payload, cancellationToken);
        return Serialize(result);
    }

    private async Task<JsonObject> IndicatorCheckinDeleteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var indicatorId = ParseGuid(arguments, "indicatorId");
        var checkinId = ParseGuid(arguments, "checkinId");
        await _budApiClient.DeleteIndicatorCheckinAsync(indicatorId, checkinId, cancellationToken);
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

            if (!SchemaTypeContains(propertySchema, "object"))
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
        var schemaForHelp = EnrichSchemaForHelp(tool.Name, tool.InputSchema.DeepClone()!.AsObject());
        var required = new JsonArray();
        if (schemaForHelp["required"] is JsonArray requiredFromSchema)
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
            ["inputSchema"] = schemaForHelp,
            ["example"] = BuildToolExample(tool.Name),
            ["responseExample"] = BuildToolResponseExample(tool.Name)
        };
    }

    private static JsonObject EnrichSchemaForHelp(string toolName, JsonObject schema)
    {
        var clone = schema.DeepClone()!.AsObject();

        switch (toolName)
        {
            case "goal_create":
            case "goal_update":
                ApplyEnumMetadata(clone, "status", typeof(GoalStatus));
                ApplyEnumMetadata(clone, "scopeType", typeof(GoalScopeType));
                ApplyNullableHint(clone, "description");
                break;
            case "goal_list":
                ApplyEnumMetadata(clone, "scopeType", typeof(GoalScopeType));
                break;
            case "goal_indicator_create":
            case "goal_indicator_update":
                ApplyEnumMetadata(clone, "type", typeof(IndicatorType));
                ApplyEnumMetadata(clone, "quantitativeType", typeof(QuantitativeIndicatorType));
                ApplyEnumMetadata(clone, "unit", typeof(IndicatorUnit));
                ApplyNullableHint(clone, "quantitativeType");
                ApplyNullableHint(clone, "minValue");
                ApplyNullableHint(clone, "maxValue");
                ApplyNullableHint(clone, "unit");
                ApplyNullableHint(clone, "targetText");
                break;
            case "indicator_checkin_create":
            case "indicator_checkin_update":
                ApplyConfidenceLevelMetadata(clone);
                ApplyNullableHint(clone, "value");
                ApplyNullableHint(clone, "text");
                ApplyNullableHint(clone, "note");
                break;
        }

        if (toolName == "goal_update" || toolName == "goal_indicator_update" || toolName == "indicator_checkin_update")
        {
            if (TryGetPropertySchema(clone, "payload", out var payloadSchema))
            {
                var enrichedPayload = EnrichSchemaForHelp(toolName.Replace("_update", "_create", StringComparison.Ordinal), payloadSchema);
                var properties = clone["properties"]?.AsObject();
                if (properties is not null)
                {
                    properties["payload"] = enrichedPayload;
                }
            }
        }

        return clone;
    }

    private static void ApplyConfidenceLevelMetadata(JsonObject schema)
    {
        if (!TryGetPropertySchema(schema, "confidenceLevel", out var confidenceSchema))
        {
            return;
        }

        confidenceSchema["minimum"] = 1;
        confidenceSchema["maximum"] = 5;
        confidenceSchema["x-value-meaning"] = new JsonObject
        {
            ["1"] = "Muito baixa",
            ["2"] = "Baixa",
            ["3"] = "Média",
            ["4"] = "Alta",
            ["5"] = "Muito alta"
        };
    }

    private static void ApplyNullableHint(JsonObject schema, string propertyPath)
    {
        if (!TryGetPropertySchema(schema, propertyPath, out var propertySchema))
        {
            return;
        }

        if (SchemaTypeContains(propertySchema, "null"))
        {
            propertySchema["x-null-handling"] = "Aceita null ou omissão do campo.";
        }
    }

    private static void ApplyEnumMetadata(JsonObject schema, string propertyPath, Type enumType)
    {
        if (!enumType.IsEnum || !TryGetPropertySchema(schema, propertyPath, out var propertySchema))
        {
            return;
        }

        var enumValues = new JsonArray();
        var enumNames = new JsonArray();
        var enumMap = new JsonObject();

        foreach (var value in Enum.GetValues(enumType))
        {
            var intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            var name = Enum.GetName(enumType, value) ?? intValue.ToString(CultureInfo.InvariantCulture);
            enumValues.Add(intValue);
            enumNames.Add(name);
            enumMap[intValue.ToString(CultureInfo.InvariantCulture)] = name;
        }

        propertySchema["enum"] = enumValues;
        propertySchema["x-enumNames"] = enumNames;
        propertySchema["x-enumMap"] = enumMap;
    }

    private static bool TryGetPropertySchema(JsonObject schema, string propertyPath, out JsonObject propertySchema)
    {
        propertySchema = null!;

        var segments = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonObject? currentSchema = schema;

        for (var i = 0; i < segments.Length; i++)
        {
            if (currentSchema?["properties"] is not JsonObject properties ||
                properties[segments[i]] is not JsonObject nextSchema)
            {
                return false;
            }

            if (i == segments.Length - 1)
            {
                propertySchema = nextSchema;
                return true;
            }

            currentSchema = nextSchema;
        }

        return false;
    }

    private static bool SchemaTypeContains(JsonObject schema, string expectedType)
    {
        if (schema["type"] is JsonValue typeValue)
        {
            return string.Equals(typeValue.GetValue<string>(), expectedType, StringComparison.Ordinal);
        }

        if (schema["type"] is JsonArray typeArray)
        {
            foreach (var item in typeArray)
            {
                if (item is JsonValue itemValue &&
                    string.Equals(itemValue.GetValue<string>(), expectedType, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
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
            "help_list_actions" => new JsonObject(),
            "help_action_schema" => new JsonObject { ["action"] = "goal_create" },
            "goal_create" => new JsonObject
            {
                ["name"] = "teste do claude",
                ["description"] = "Meta criada via MCP",
                ["startDate"] = "2026-02-08T00:00:00Z",
                ["endDate"] = "2026-02-15T00:00:00Z",
                ["status"] = "Planned",
                ["scopeType"] = "Organization",
                ["scopeId"] = "00000000-0000-0000-0000-000000000001"
            },
            "goal_get" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000002" },
            "goal_list" => new JsonObject { ["scopeType"] = "Organization", ["scopeId"] = "00000000-0000-0000-0000-000000000001", ["page"] = 1, ["pageSize"] = 10 },
            "goal_update" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000002",
                ["payload"] = new JsonObject
                {
                    ["name"] = "Meta atualizada",
                    ["description"] = "Ajuste de escopo",
                    ["startDate"] = "2026-02-08T00:00:00Z",
                    ["endDate"] = "2026-02-20T00:00:00Z",
                    ["status"] = "Active",
                    ["scopeType"] = "Organization",
                    ["scopeId"] = "00000000-0000-0000-0000-000000000001"
                }
            },
            "goal_delete" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000002" },
            "goal_indicator_create" => new JsonObject { ["goalId"] = "00000000-0000-0000-0000-000000000002", ["name"] = "NPS", ["type"] = "Quantitative", ["quantitativeType"] = "KeepAbove", ["minValue"] = 80, ["unit"] = "Percentage" },
            "goal_indicator_get" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000003" },
            "goal_indicator_list" => new JsonObject { ["goalId"] = "00000000-0000-0000-0000-000000000002", ["page"] = 1, ["pageSize"] = 10 },
            "goal_indicator_update" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000003", ["payload"] = new JsonObject { ["name"] = "NPS trimestral", ["type"] = "Quantitative", ["quantitativeType"] = "KeepAbove", ["minValue"] = 85, ["unit"] = "Percentage" } },
            "goal_indicator_delete" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000003" },
            "indicator_checkin_create" => new JsonObject { ["indicatorId"] = "00000000-0000-0000-0000-000000000003", ["value"] = 86.5, ["checkinDate"] = "2026-02-08T00:00:00Z", ["note"] = "Evolução semanal", ["confidenceLevel"] = 4 },
            "indicator_checkin_get" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000004" },
            "indicator_checkin_list" => new JsonObject { ["indicatorId"] = "00000000-0000-0000-0000-000000000003", ["page"] = 1, ["pageSize"] = 10 },
            "indicator_checkin_update" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000004", ["payload"] = new JsonObject { ["value"] = 88.0, ["checkinDate"] = "2026-02-09T00:00:00Z", ["note"] = "Ajuste após revisão", ["confidenceLevel"] = 5 } },
            "indicator_checkin_delete" => new JsonObject { ["id"] = "00000000-0000-0000-0000-000000000004" },
            _ => new JsonObject()
        };
    }

    private static JsonObject BuildToolResponseExample(string toolName)
    {
        return toolName switch
        {
            "auth_login" => new JsonObject
            {
                ["whoami"] = new JsonObject
                {
                    ["email"] = "admin@getbud.co",
                    ["isGlobalAdmin"] = true,
                    ["organizationId"] = null
                },
                ["requiresTenantForDomainTools"] = true,
                ["nextSteps"] = new JsonArray("tenant_list_available", "tenant_set_current", "help_list_actions", "help_action_schema", "session_bootstrap")
            },
            "auth_whoami" => new JsonObject
            {
                ["email"] = "admin@getbud.co",
                ["isGlobalAdmin"] = true
            },
            "tenant_list_available" => new JsonObject
            {
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["id"] = "00000000-0000-0000-0000-000000000001",
                        ["name"] = "Acme"
                    }
                },
                ["currentTenantId"] = "00000000-0000-0000-0000-000000000001"
            },
            "tenant_set_current" => new JsonObject
            {
                ["tenantId"] = "00000000-0000-0000-0000-000000000001",
                ["message"] = "Tenant atualizado com sucesso."
            },
            "session_bootstrap" => new JsonObject
            {
                ["requiresTenantForDomainTools"] = true,
                ["nextSteps"] = new JsonArray("tenant_set_current", "help_list_actions", "help_action_schema")
            },
            "help_list_actions" => new JsonObject
            {
                ["actions"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "mission_create",
                        ["description"] = "Cria uma missão."
                    }
                }
            },
            "help_action_schema" => new JsonObject
            {
                ["name"] = "goal_create",
                ["required"] = new JsonArray("name", "startDate", "endDate", "status", "scopeType", "scopeId")
            },
            "goal_create" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000002",
                ["name"] = "teste do claude",
                ["status"] = "Planned"
            },
            "goal_get" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000002",
                ["name"] = "teste do claude",
                ["status"] = "Planned"
            },
            "goal_list" => new JsonObject
            {
                ["items"] = new JsonArray(),
                ["page"] = 1,
                ["pageSize"] = 10
            },
            "goal_update" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000002",
                ["name"] = "Meta atualizada",
                ["status"] = "Active"
            },
            "goal_delete" => new JsonObject { ["deleted"] = true },
            "goal_indicator_create" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000003",
                ["name"] = "NPS",
                ["type"] = "Quantitative"
            },
            "goal_indicator_get" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000003",
                ["name"] = "NPS",
                ["type"] = "Quantitative"
            },
            "goal_indicator_list" => new JsonObject
            {
                ["items"] = new JsonArray(),
                ["page"] = 1,
                ["pageSize"] = 10
            },
            "goal_indicator_update" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000003",
                ["name"] = "NPS trimestral",
                ["type"] = "Quantitative"
            },
            "goal_indicator_delete" => new JsonObject { ["deleted"] = true },
            "indicator_checkin_create" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000004",
                ["confidenceLevel"] = 4
            },
            "indicator_checkin_get" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000004",
                ["confidenceLevel"] = 4
            },
            "indicator_checkin_list" => new JsonObject
            {
                ["items"] = new JsonArray(),
                ["page"] = 1,
                ["pageSize"] = 10
            },
            "indicator_checkin_update" => new JsonObject
            {
                ["id"] = "00000000-0000-0000-0000-000000000004",
                ["confidenceLevel"] = 5
            },
            "indicator_checkin_delete" => new JsonObject { ["deleted"] = true },
            _ => new JsonObject()
        };
    }
}
