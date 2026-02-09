using System.Net;
using Bud.Mcp.Configuration;
using Bud.Mcp.Tests.Helpers;
using Bud.Mcp.Tools.Generation;

namespace Bud.Mcp.Tests.Tools.Generation;

[Collection(CatalogFileCollectionDefinition.Name)]
public sealed class ToolCatalogCommandRunnerTests
{
    [Fact]
    public async Task TryExecuteAsync_WhenCommandIsUnknown_ReturnsNotHandled()
    {
        var runner = new ToolCatalogCommandRunner(_ => CreateHttpClient(SampleOpenApi));
        var options = new BudMcpOptions("http://localhost:8080", null, null, 30);

        var result = await runner.TryExecuteAsync(["noop"], options);

        result.Handled.Should().BeFalse();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task TryExecuteAsync_GenerateToolCatalog_WritesCatalogAndReturnsSuccess()
    {
        var runner = new ToolCatalogCommandRunner(_ => CreateHttpClient(SampleOpenApi));
        var options = new BudMcpOptions("http://localhost:8080", null, null, 30);

        await WithCatalogBackupAsync(async () =>
        {
            var result = await runner.TryExecuteAsync(["generate-tool-catalog"], options);

            result.Handled.Should().BeTrue();
            result.ExitCode.Should().Be(0);

            var generated = await McpToolCatalogStore.TryReadRawAsync();
            generated.Should().NotBeNullOrWhiteSpace();
            generated.Should().Contain("mission_create");
        });
    }

    [Fact]
    public async Task TryExecuteAsync_CheckToolCatalog_WithFailOnDiff_ReturnsFailure()
    {
        var runner = new ToolCatalogCommandRunner(_ => CreateHttpClient(SampleOpenApi));
        var options = new BudMcpOptions("http://localhost:8080", null, null, 30);

        await WithCatalogBackupAsync(async () =>
        {
            await McpToolCatalogStore.WriteAsync("""
            {
              "version": 1,
              "tools": []
            }
            """);

            var result = await runner.TryExecuteAsync(["check-tool-catalog", "--fail-on-diff"], options);
            result.Handled.Should().BeTrue();
            result.ExitCode.Should().Be(1);
        });
    }

    [Fact]
    public async Task TryExecuteAsync_CheckToolCatalog_WhenRequiredContractIsInvalid_ReturnsFailure()
    {
        var runner = new ToolCatalogCommandRunner(_ => CreateHttpClient(InvalidRequiredOpenApi));
        var options = new BudMcpOptions("http://localhost:8080", null, null, 30);

        await WithCatalogBackupAsync(async () =>
        {
            var currentCatalog = OpenApiToolCatalogGenerator.BuildCatalogJson(InvalidRequiredOpenApi);
            await McpToolCatalogStore.WriteAsync(currentCatalog);

            var result = await runner.TryExecuteAsync(["check-tool-catalog"], options);
            result.Handled.Should().BeTrue();
            result.ExitCode.Should().Be(1);
        });
    }

    [Fact]
    public async Task TryExecuteAsync_CheckToolCatalog_WhenUpToDateAndValid_ReturnsSuccess()
    {
        var runner = new ToolCatalogCommandRunner(_ => CreateHttpClient(SampleOpenApi));
        var options = new BudMcpOptions("http://localhost:8080", null, null, 30);

        await WithCatalogBackupAsync(async () =>
        {
            var currentCatalog = OpenApiToolCatalogGenerator.BuildCatalogJson(SampleOpenApi);
            await McpToolCatalogStore.WriteAsync(currentCatalog);

            var result = await runner.TryExecuteAsync(["check-tool-catalog", "--fail-on-diff"], options);
            result.Handled.Should().BeTrue();
            result.ExitCode.Should().Be(0);
        });
    }

    private static HttpClient CreateHttpClient(string responseJson)
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
    }

    private static async Task WithCatalogBackupAsync(Func<Task> testAction)
    {
        var path = McpToolCatalogStore.ResolveCatalogPath();
        var originalExists = File.Exists(path);
        var original = originalExists ? await File.ReadAllTextAsync(path) : null;

        try
        {
            await testAction();
        }
        finally
        {
            if (originalExists)
            {
                await File.WriteAllTextAsync(path, original!);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
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
          "get": { "parameters": [] }
        },
        "/api/missions/{id}": {
          "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "put": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/UpdateMissionRequest" } } } } },
          "delete": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
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
            "required": ["name"],
            "properties": {
              "name": { "type": "string" }
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
            "required": ["name"],
            "properties": {
              "name": { "type": "string" }
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
            "required": ["checkinDate"],
            "properties": {
              "checkinDate": { "type": "string", "format": "date-time" }
            }
          }
        }
      }
    }
    """;

    private const string InvalidRequiredOpenApi = """
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
          "get": { "parameters": [] }
        },
        "/api/missions/{id}": {
          "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] },
          "put": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ], "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/UpdateMissionRequest" } } } } },
          "delete": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } } ] }
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
            "required": ["name"],
            "properties": {
              "name": { "type": "string" }
            }
          },
          "UpdateMissionRequest": {
            "type": "object",
            "required": ["name"],
            "properties": {
              "name": { "type": "string" }
            }
          },
          "CreateMissionMetricRequest": {
            "type": "object",
            "required": ["missionId"],
            "properties": {
              "missionId": { "type": "string", "format": "uuid" }
            }
          },
          "UpdateMissionMetricRequest": {
            "type": "object",
            "required": ["name"],
            "properties": {
              "name": { "type": "string" }
            }
          },
          "CreateMetricCheckinRequest": {
            "type": "object",
            "required": ["missionMetricId"],
            "properties": {
              "missionMetricId": { "type": "string", "format": "uuid" }
            }
          },
          "UpdateMetricCheckinRequest": {
            "type": "object",
            "required": ["checkinDate"],
            "properties": {
              "checkinDate": { "type": "string", "format": "date-time" }
            }
          }
        }
      }
    }
    """;
}
