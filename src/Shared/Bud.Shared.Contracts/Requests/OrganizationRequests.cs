using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.Requests;

public sealed class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
}

public sealed class PatchOrganizationRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<Guid?> OwnerId { get; set; }
}
