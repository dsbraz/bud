using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.People;

public sealed class CreateCollaboratorRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; } = CollaboratorRole.IndividualContributor;
    public Guid? TeamId { get; set; }
    public Guid? LeaderId { get; set; }
}

public sealed class PatchCollaboratorRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> FullName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Email { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<CollaboratorRole> Role { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<Guid?> LeaderId { get; set; }
}

public sealed class PatchCollaboratorTeamsRequest
{
    public List<Guid> TeamIds { get; set; } = [];
}
