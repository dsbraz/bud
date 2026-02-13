namespace Bud.Server.Services;

/// <summary>
/// Orquestra a criacao de notificacoes para eventos de dominio,
/// encapsulando a resolucao de destinatarios e a criacao das notificacoes.
/// </summary>
public interface INotificationOrchestrator
{
    /// <summary>
    /// Notifica os destinatarios sobre a criacao de uma missao.
    /// </summary>
    Task NotifyMissionCreatedAsync(
        Guid missionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifica os destinatarios sobre a atualizacao de uma missao.
    /// </summary>
    Task NotifyMissionUpdatedAsync(
        Guid missionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifica os destinatarios sobre a exclusao de uma missao.
    /// </summary>
    Task NotifyMissionDeletedAsync(
        Guid missionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifica os destinatarios sobre a criacao de um check-in de metrica.
    /// </summary>
    /// <param name="checkinId">ID do check-in criado.</param>
    /// <param name="missionMetricId">ID da metrica associada ao check-in.</param>
    /// <param name="organizationId">ID da organizacao.</param>
    /// <param name="excludeCollaboratorId">Colaborador a ser excluido da notificacao (geralmente o autor).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task NotifyMetricCheckinCreatedAsync(
        Guid checkinId,
        Guid missionMetricId,
        Guid organizationId,
        Guid? excludeCollaboratorId,
        CancellationToken cancellationToken = default);
}
