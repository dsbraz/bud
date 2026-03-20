namespace Bud.Shared.Contracts.Organizations;

public sealed class OrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Plan { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
