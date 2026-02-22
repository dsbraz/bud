using Bud.Server.Application.Common;
using Bud.Server.Domain.Repositories;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Collaborators;

public sealed class GetCollaboratorHierarchy(ICollaboratorRepository collaboratorRepository)
{
    public async Task<Result<List<CollaboratorHierarchyNodeDto>>> ExecuteAsync(
        Guid collaboratorId,
        CancellationToken cancellationToken = default)
    {
        if (!await collaboratorRepository.ExistsAsync(collaboratorId, cancellationToken))
        {
            return Result<List<CollaboratorHierarchyNodeDto>>.NotFound("Colaborador não encontrado.");
        }

        var subordinates = await collaboratorRepository.GetSubordinatesAsync(collaboratorId, 5, cancellationToken);
        var childrenByLeader = subordinates
            .Where(c => c.LeaderId.HasValue)
            .GroupBy(c => c.LeaderId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(c => c.FullName).ToList());

        var tree = BuildTree(collaboratorId, childrenByLeader, 0, 5);
        return Result<List<CollaboratorHierarchyNodeDto>>.Success(tree);
    }

    private static List<CollaboratorHierarchyNodeDto> BuildTree(
        Guid leaderId,
        Dictionary<Guid, List<Bud.Server.Domain.Model.Collaborator>> childrenByLeader,
        int depth,
        int maxDepth)
    {
        if (depth >= maxDepth || !childrenByLeader.TryGetValue(leaderId, out var children))
        {
            return [];
        }

        return children
            .Select(collaborator => new CollaboratorHierarchyNodeDto
            {
                Id = collaborator.Id,
                FullName = collaborator.FullName,
                Initials = GetInitials(collaborator.FullName),
                Role = collaborator.Role == Bud.Server.Domain.Model.CollaboratorRole.Leader ? "Líder" : "Contribuidor individual",
                Children = BuildTree(collaborator.Id, childrenByLeader, depth + 1, maxDepth)
            })
            .ToList();
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..1].ToUpperInvariant();
        }

        return $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
    }
}
