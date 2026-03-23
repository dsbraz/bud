namespace Bud.Application.Features.Teams;

public sealed class TeamMemberSnapshot
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}
