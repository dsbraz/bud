namespace Bud.Shared.Contracts.Features.Organizations;

public sealed class OrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public CollaboratorResponse? Owner { get; set; }
}
