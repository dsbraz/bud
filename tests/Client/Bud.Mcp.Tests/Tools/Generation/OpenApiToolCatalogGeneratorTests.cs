using System.Text.Json.Nodes;
using Bud.Mcp.Tools.Generation;

namespace Bud.Mcp.Tests.Tools.Generation;

public sealed class OpenApiToolCatalogGeneratorTests
{
    private static readonly string[] MissionCreateRequiredFields = ["name", "startDate", "endDate", "status"];
    private static readonly string[] MissionUpdateRequiredFields = ["id", "payload"];
    private static readonly string[] IndicatorCreateRequiredFields = ["goalId", "name", "type"];

    [Fact]
    public void BuildCatalogJson_GeneratesMissionCreateSchemaWithRequiredFields()
    {
        var json = OpenApiToolCatalogGenerator.BuildCatalogJson(SampleOpenApi);
        var root = JsonNode.Parse(json)!.AsObject();
        var tools = root["tools"]!.AsArray();
        var missionCreate = tools.Single(tool => tool!["name"]!.GetValue<string>() == "goal_create")!.AsObject();

        var schema = missionCreate["inputSchema"]!.AsObject();
        schema["type"]!.GetValue<string>().Should().Be("object");
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>());
        required.Should().Contain(MissionCreateRequiredFields);
    }

    [Fact]
    public void BuildCatalogJson_GeneratesMissionUpdateSchemaWithIdAndPayload()
    {
        var json = OpenApiToolCatalogGenerator.BuildCatalogJson(SampleOpenApi);
        var root = JsonNode.Parse(json)!.AsObject();
        var tools = root["tools"]!.AsArray();
        var missionUpdate = tools.Single(tool => tool!["name"]!.GetValue<string>() == "goal_update")!.AsObject();

        var schema = missionUpdate["inputSchema"]!.AsObject();
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>());
        required.Should().Contain(MissionUpdateRequiredFields);
        schema["properties"]!["payload"]!["type"]!.GetValue<string>().Should().Be("object");
    }

    [Fact]
    public void BuildCatalogJson_GeneratesIndicatorCreateSchemaWithRequiredFields()
    {
        var json = OpenApiToolCatalogGenerator.BuildCatalogJson(SampleOpenApi);
        var root = JsonNode.Parse(json)!.AsObject();
        var tools = root["tools"]!.AsArray();
        var indicatorCreate = tools.Single(tool => tool!["name"]!.GetValue<string>() == "goal_indicator_create")!.AsObject();

        var schema = indicatorCreate["inputSchema"]!.AsObject();
        schema["type"]!.GetValue<string>().Should().Be("object");
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>());
        required.Should().Contain(IndicatorCreateRequiredFields);
    }

    [Fact]
    public void BuildCatalogJson_InfersRequiredFromNonNullableProperties_WhenOpenApiOmitsRequired()
    {
        var json = OpenApiToolCatalogGenerator.BuildCatalogJson(SampleOpenApiWithoutRequired);
        var root = JsonNode.Parse(json)!.AsObject();
        var tools = root["tools"]!.AsArray();
        var missionCreate = tools.Single(tool => tool!["name"]!.GetValue<string>() == "goal_create")!.AsObject();

        var schema = missionCreate["inputSchema"]!.AsObject();
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.Should().Contain("name");
        required.Should().Contain("startDate");
        required.Should().NotContain("description");
    }

    private const string SampleOpenApi = """
    {
      "openapi": "3.0.1",
      "paths": {
        "/api/goals": {
          "post": {
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "$ref": "#/components/schemas/CreateGoalRequest"
                  }
                }
              }
            }
          },
          "get": {
            "parameters": [
              { "name": "filter", "in": "query", "schema": { "type": "string" } },
              { "name": "page", "in": "query", "schema": { "type": "integer", "default": 1 } },
              { "name": "pageSize", "in": "query", "schema": { "type": "integer", "default": 10 } }
            ]
          }
        },
        "/api/goals/{id}": {
          "get": {
            "parameters": [
              { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
            ]
          },
          "patch": {
            "parameters": [
              { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
            ],
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "$ref": "#/components/schemas/PatchGoalRequest"
                  }
                }
              }
            }
          },
          "delete": {
            "parameters": [
              { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
            ]
          }
        },
        "/api/indicators": {
          "post": { "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreateIndicatorRequest" } } } } },
          "get": { "parameters": [] }
        },
        "/api/indicators/{id}": {
          "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "patch": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/PatchIndicatorRequest" } } } } },
          "delete": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
        },
        "/api/indicators/{indicatorId}/checkins": {
          "post": { "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreateCheckinRequest" } } } } },
          "get": { "parameters": [] }
        },
        "/api/indicators/{indicatorId}/checkins/{checkinId}": {
          "get": { "parameters": [ { "name": "indicatorId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }, { "name": "checkinId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "patch": { "parameters": [ { "name": "indicatorId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }, { "name": "checkinId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/PatchCheckinRequest" } } } } },
          "delete": { "parameters": [ { "name": "indicatorId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }, { "name": "checkinId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
        }
      },
      "components": {
        "schemas": {
          "CreateGoalRequest": {
            "type": "object",
            "required": ["name", "startDate", "endDate", "status"],
            "properties": {
              "name": { "type": "string" },
              "startDate": { "type": "string", "format": "date-time" },
              "endDate": { "type": "string", "format": "date-time" },
              "status": { "type": "integer", "format": "int32" }
            }
          },
          "PatchGoalRequest": {
            "type": "object",
            "required": ["name", "startDate", "endDate", "status"],
            "properties": {
              "name": { "type": "string" },
              "startDate": { "type": "string", "format": "date-time" },
              "endDate": { "type": "string", "format": "date-time" },
              "status": { "type": "integer", "format": "int32" }
            }
          },
          "CreateIndicatorRequest": {
            "type": "object",
            "required": ["goalId", "name", "type"],
            "properties": {
              "goalId": { "type": "string", "format": "uuid" },
              "name": { "type": "string" },
              "type": { "type": "integer", "format": "int32" }
            }
          },
          "PatchIndicatorRequest": {
            "type": "object",
            "required": ["name", "type"],
            "properties": {
              "name": { "type": "string" },
              "type": { "type": "integer", "format": "int32" }
            }
          },
          "CreateCheckinRequest": {
            "type": "object",
            "required": ["checkinDate", "confidenceLevel"],
            "properties": {
              "checkinDate": { "type": "string", "format": "date-time" },
              "confidenceLevel": { "type": "integer", "format": "int32" }
            }
          },
          "PatchCheckinRequest": {
            "type": "object",
            "required": ["checkinDate", "confidenceLevel"],
            "properties": {
              "checkinDate": { "type": "string", "format": "date-time" },
              "confidenceLevel": { "type": "integer", "format": "int32" }
            }
          }
        }
      }
    }
    """;

    private const string SampleOpenApiWithoutRequired = """
    {
      "openapi": "3.0.1",
      "paths": {
        "/api/goals": {
          "post": {
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "$ref": "#/components/schemas/CreateGoalRequest"
                  }
                }
              }
            }
          },
          "get": { "parameters": [] }
        },
        "/api/goals/{id}": {
          "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "patch": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/PatchGoalRequest" } } } } },
          "delete": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
        },
        "/api/indicators": {
          "post": { "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreateIndicatorRequest" } } } } },
          "get": { "parameters": [] }
        },
        "/api/indicators/{id}": {
          "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "patch": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/PatchIndicatorRequest" } } } } },
          "delete": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
        },
        "/api/indicators/{indicatorId}/checkins": {
          "post": { "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreateCheckinRequest" } } } } },
          "get": { "parameters": [] }
        },
        "/api/indicators/{indicatorId}/checkins/{checkinId}": {
          "get": { "parameters": [ { "name": "indicatorId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }, { "name": "checkinId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "patch": { "parameters": [ { "name": "indicatorId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }, { "name": "checkinId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/PatchCheckinRequest" } } } } },
          "delete": { "parameters": [ { "name": "indicatorId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }, { "name": "checkinId", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
        }
      },
      "components": {
        "schemas": {
          "CreateGoalRequest": {
            "type": "object",
            "properties": {
              "name": { "type": "string" },
              "description": { "type": ["null", "string"] },
              "startDate": { "type": "string", "format": "date-time" },
              "endDate": { "type": "string", "format": "date-time" },
              "status": { "type": "integer", "format": "int32" }
            }
          },
          "PatchGoalRequest": {
            "type": "object",
            "properties": {
              "name": { "type": "string" },
              "startDate": { "type": "string", "format": "date-time" },
              "endDate": { "type": "string", "format": "date-time" },
              "status": { "type": "integer", "format": "int32" }
            }
          },
          "CreateIndicatorRequest": {
            "type": "object",
            "properties": {
              "goalId": { "type": "string", "format": "uuid" },
              "name": { "type": "string" },
              "type": { "type": "integer", "format": "int32" }
            }
          },
          "PatchIndicatorRequest": {
            "type": "object",
            "properties": {
              "name": { "type": "string" },
              "type": { "type": "integer", "format": "int32" }
            }
          },
          "CreateCheckinRequest": {
            "type": "object",
            "properties": {
              "checkinDate": { "type": "string", "format": "date-time" },
              "confidenceLevel": { "type": "integer", "format": "int32" }
            }
          },
          "PatchCheckinRequest": {
            "type": "object",
            "properties": {
              "checkinDate": { "type": "string", "format": "date-time" },
              "confidenceLevel": { "type": "integer", "format": "int32" }
            }
          }
        }
      }
    }
    """;
}
