namespace Bud.Shared.Contracts.Features.Workspaces;

public sealed class WorkspaceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public OrganizationResponse? Organization { get; set; }
    public List<TeamResponse> Teams { get; set; } = [];
}
