using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.Features.Workspaces;

public sealed class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
}

public sealed class PatchWorkspaceRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Name { get; set; }
}
