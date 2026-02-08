using Bud.Mcp.Tools.Generation;
using System.Text.Json.Nodes;

namespace Bud.Mcp.Tests.Tools.Generation;

public sealed class OpenApiToolCatalogGeneratorTests
{
    private static readonly string[] MissionCreateRequiredFields = ["name", "startDate", "endDate", "status", "scopeType", "scopeId"];
    private static readonly string[] MissionUpdateRequiredFields = ["id", "payload"];

    [Fact]
    public void BuildCatalogJson_GeneratesMissionCreateSchemaWithRequiredFields()
    {
        var json = OpenApiToolCatalogGenerator.BuildCatalogJson(SampleOpenApi);
        var root = JsonNode.Parse(json)!.AsObject();
        var tools = root["tools"]!.AsArray();
        var missionCreate = tools.Single(tool => tool!["name"]!.GetValue<string>() == "mission_create")!.AsObject();

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
        var missionUpdate = tools.Single(tool => tool!["name"]!.GetValue<string>() == "mission_update")!.AsObject();

        var schema = missionUpdate["inputSchema"]!.AsObject();
        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>());
        required.Should().Contain(MissionUpdateRequiredFields);
        schema["properties"]!["payload"]!["type"]!.GetValue<string>().Should().Be("object");
    }

    private const string SampleOpenApi = """
    {
      "openapi": "3.0.1",
      "paths": {
        "/api/missions": {
          "post": {
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "$ref": "#/components/schemas/CreateMissionRequest"
                  }
                }
              }
            }
          },
          "get": {
            "parameters": [
              { "name": "scopeType", "in": "query", "schema": { "type": "string" } },
              { "name": "scopeId", "in": "query", "schema": { "type": "string", "format": "uuid" } },
              { "name": "page", "in": "query", "schema": { "type": "integer", "default": 1 } },
              { "name": "pageSize", "in": "query", "schema": { "type": "integer", "default": 10 } }
            ]
          }
        },
        "/api/missions/{id}": {
          "get": {
            "parameters": [
              { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
            ]
          },
          "put": {
            "parameters": [
              { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
            ],
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "$ref": "#/components/schemas/UpdateMissionRequest"
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
        "/api/mission-metrics": {
          "post": { "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreateMissionMetricRequest" } } } } },
          "get": { "parameters": [] }
        },
        "/api/mission-metrics/{id}": {
          "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "put": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/UpdateMissionMetricRequest" } } } } },
          "delete": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
        },
        "/api/metric-checkins": {
          "post": { "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreateMetricCheckinRequest" } } } } },
          "get": { "parameters": [] }
        },
        "/api/metric-checkins/{id}": {
          "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "put": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/UpdateMetricCheckinRequest" } } } } },
          "delete": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
        }
      },
      "components": {
        "schemas": {
          "CreateMissionRequest": {
            "type": "object",
            "required": ["name", "startDate", "endDate", "status", "scopeType", "scopeId"],
            "properties": {
              "name": { "type": "string" },
              "startDate": { "type": "string", "format": "date-time" },
              "endDate": { "type": "string", "format": "date-time" },
              "status": { "type": "integer", "format": "int32" },
              "scopeType": { "type": "integer", "format": "int32" },
              "scopeId": { "type": "string", "format": "uuid" }
            }
          },
          "UpdateMissionRequest": {
            "type": "object",
            "required": ["name", "startDate", "endDate", "status", "scopeType", "scopeId"],
            "properties": {
              "name": { "type": "string" },
              "startDate": { "type": "string", "format": "date-time" },
              "endDate": { "type": "string", "format": "date-time" },
              "status": { "type": "integer", "format": "int32" },
              "scopeType": { "type": "integer", "format": "int32" },
              "scopeId": { "type": "string", "format": "uuid" }
            }
          },
          "CreateMissionMetricRequest": {
            "type": "object",
            "required": ["missionId", "name", "type"],
            "properties": {
              "missionId": { "type": "string", "format": "uuid" },
              "name": { "type": "string" },
              "type": { "type": "integer", "format": "int32" }
            }
          },
          "UpdateMissionMetricRequest": {
            "type": "object",
            "required": ["name", "type"],
            "properties": {
              "name": { "type": "string" },
              "type": { "type": "integer", "format": "int32" }
            }
          },
          "CreateMetricCheckinRequest": {
            "type": "object",
            "required": ["missionMetricId", "checkinDate", "confidenceLevel"],
            "properties": {
              "missionMetricId": { "type": "string", "format": "uuid" },
              "checkinDate": { "type": "string", "format": "date-time" },
              "confidenceLevel": { "type": "integer", "format": "int32" }
            }
          },
          "UpdateMetricCheckinRequest": {
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
}
