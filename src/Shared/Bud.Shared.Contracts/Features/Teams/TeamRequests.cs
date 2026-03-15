using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.Features.Teams;

public sealed class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public Guid LeaderId { get; set; }
    public Guid? ParentTeamId { get; set; }
}

public sealed class PatchTeamRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<Guid> LeaderId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<Guid?> ParentTeamId { get; set; }
}

public sealed class PatchTeamCollaboratorsRequest
{
    public List<Guid> CollaboratorIds { get; set; } = [];
}
