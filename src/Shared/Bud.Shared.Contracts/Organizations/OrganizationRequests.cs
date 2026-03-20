using Bud.Shared.Kernel;
using System.Text.Json.Serialization;

namespace Bud.Shared.Contracts.Organizations;

public sealed class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Plan { get; set; }
    public string? IconUrl { get; set; }
}

public sealed class PatchOrganizationRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string> Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> Plan { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Optional<string?> IconUrl { get; set; }
}
