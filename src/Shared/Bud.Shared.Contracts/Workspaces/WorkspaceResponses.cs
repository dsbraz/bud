namespace Bud.Shared.Contracts.Workspaces;

public sealed class WorkspaceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public OrganizationResponse? Organization { get; set; }
    public List<TeamResponse> Teams { get; set; } = [];
}
