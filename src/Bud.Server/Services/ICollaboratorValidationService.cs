namespace Bud.Server.Application.Abstractions;

public interface ICollaboratorValidationService
{
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsValidLeaderForCreateAsync(Guid leaderId, CancellationToken cancellationToken = default);

    Task<bool> IsValidLeaderForUpdateAsync(Guid leaderId, CancellationToken cancellationToken = default);
}
