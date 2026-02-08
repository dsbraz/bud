using Bud.Mcp.Tools.Generation;

namespace Bud.Mcp.Tests.Tools.Generation;

[Collection(CatalogFileCollectionDefinition.Name)]
public sealed class McpToolCatalogStoreTests
{
    [Fact]
    public void ParseToolsFromCatalogJson_ReturnsDomainToolsFromCatalog()
    {
        const string catalogJson = """
        {
          "version": 1,
          "tools": [
            {
              "name": "mission_create",
              "description": "Cria uma missão.",
              "inputSchema": {
                "type": "object",
                "properties": {
                  "name": { "type": "string" }
                }
              }
            },
            {
              "name": "mission_get",
              "description": "Busca por id.",
              "inputSchema": {
                "type": "object",
                "required": [ "id" ],
                "properties": {
                  "id": { "type": "string", "format": "uuid" }
                }
              }
            }
          ]
        }
        """;

        var tools = McpToolCatalogStore.ParseToolsFromCatalogJson(catalogJson);

        tools.Should().HaveCount(2);
        tools.Select(tool => tool.Name).Should().Contain(["mission_create", "mission_get"]);
    }

    [Fact]
    public void ParseToolsFromCatalogJson_ReturnsEmptyWhenJsonIsInvalid()
    {
        var tools = McpToolCatalogStore.ParseToolsFromCatalogJson("not-json");
        tools.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadToolsOrThrow_WhenCatalogExists_ReturnsTools()
    {
        await WithCatalogBackupAsync(async () =>
        {
            await McpToolCatalogStore.WriteAsync("""
            {
              "version": 1,
              "tools": [
                {
                  "name": "mission_create",
                  "description": "Cria uma missão.",
                  "inputSchema": {
                    "type": "object",
                    "required": [ "name" ],
                    "properties": {
                      "name": { "type": "string" }
                    }
                  }
                }
              ]
            }
            """);

            var tools = McpToolCatalogStore.LoadToolsOrThrow();

            tools.Should().ContainSingle();
            tools[0].Name.Should().Be("mission_create");
        });
    }

    [Fact]
    public async Task LoadToolsOrThrow_WhenCatalogIsMissing_Throws()
    {
        await WithCatalogBackupAsync(async () =>
        {
            var path = McpToolCatalogStore.ResolveCatalogPath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var act = () => McpToolCatalogStore.LoadToolsOrThrow();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Catálogo MCP não encontrado*");
            await Task.CompletedTask;
        });
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
}
