using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Domain.Models;

public sealed class AggregateInvariantsTests
{
    [Fact]
    public void Organization_Rename_WithEmptyName_ShouldThrow()
    {
        var organization = Organization.Create(Guid.NewGuid(), "Org", Guid.NewGuid());

        var act = () => organization.Rename("  ");

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Organization_Rename_WithNameLongerThan200_ShouldThrow()
    {
        var organization = Organization.Create(Guid.NewGuid(), "Org", Guid.NewGuid());
        var longName = new string('A', 201);

        var act = () => organization.Rename(longName);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Workspace_Create_WithEmptyOrganization_ShouldThrow()
    {
        var act = () => Workspace.Create(Guid.NewGuid(), Guid.Empty, "Workspace");

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Team_Reparent_ToSelf_ShouldThrow()
    {
        var id = Guid.NewGuid();
        var team = Team.Create(id, Guid.NewGuid(), Guid.NewGuid(), "Team");

        var act = () => team.Reparent(id, id);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Collaborator_UpdateProfile_WithSelfLeader_ShouldThrow()
    {
        var collaborator = Collaborator.Create(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@getbud.co", CollaboratorRole.Leader);

        var act = () => collaborator.UpdateProfile("Ana", "ana@getbud.co", CollaboratorRole.Leader, collaborator.Id, collaborator.Id);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Mission_UpdateDetails_WithInvalidDateRange_ShouldThrow()
    {
        var mission = Mission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missão",
            null,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            MissionStatus.Active);

        var act = () => mission.UpdateDetails(
            "Missão",
            null,
            new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            MissionStatus.Active);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Mission_UpdateDetails_WithNameLongerThan200_ShouldThrow()
    {
        var mission = Mission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missão",
            null,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            MissionStatus.Active);

        var longName = new string('A', 201);
        var act = () => mission.UpdateDetails(
            longName,
            null,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            MissionStatus.Active);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Mission_SetScope_WithEmptyTeamId_ShouldThrow()
    {
        var mission = Mission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missão",
            null,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            MissionStatus.Active);

        var act = () => mission.SetScope(MissionScopeType.Team, Guid.Empty);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void MissionMetric_ApplyTarget_WithInvalidRange_ShouldThrow()
    {
        var metric = MissionMetric.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Métrica", MetricType.Quantitative);

        var act = () => metric.ApplyTarget(
            MetricType.Quantitative,
            QuantitativeMetricType.KeepBetween,
            100m,
            100m,
            MetricUnit.Percentage,
            null);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void MetricCheckin_Update_WithConfidenceOutOfRange_ShouldThrow()
    {
        var checkin = MetricCheckin.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10m,
            null,
            DateTime.UtcNow,
            null,
            3);

        var act = () => checkin.Update(10m, null, DateTime.UtcNow, null, 0);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void MissionMetric_CreateCheckin_WithMissingQuantitativeValue_ShouldThrow()
    {
        var metric = MissionMetric.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Métrica", MetricType.Quantitative);

        var act = () => metric.CreateCheckin(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            DateTime.UtcNow,
            null,
            3);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void MissionMetric_UpdateCheckin_WithMissingQualitativeText_ShouldThrow()
    {
        var metric = MissionMetric.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Métrica", MetricType.Qualitative);
        var checkin = MetricCheckin.Create(
            Guid.NewGuid(),
            metric.OrganizationId,
            metric.Id,
            Guid.NewGuid(),
            null,
            "Texto",
            DateTime.UtcNow,
            null,
            3);

        var act = () => metric.UpdateCheckin(checkin, null, "   ", DateTime.UtcNow, null, 3);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void MissionTemplateMetric_Create_WithQuantitativeTypeMissing_ShouldThrow()
    {
        var act = () => MissionTemplateMetric.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Receita",
            MetricType.Quantitative,
            0,
            null,
            0m,
            100m,
            MetricUnit.Percentage,
            null);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void MissionTemplate_ReplaceMetrics_ShouldSetTemplateAndOrganizationIds()
    {
        var template = MissionTemplate.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Template",
            null,
            null,
            null);

        template.ReplaceMetrics(
        [
            new MissionTemplateMetricDraft(
                "Qualidade",
                MetricType.Qualitative,
                0,
                null,
                null,
                null,
                null,
                "Meta textual")
        ]);

        template.Metrics.Should().ContainSingle();
        template.Metrics.First().MissionTemplateId.Should().Be(template.Id);
        template.Metrics.First().OrganizationId.Should().Be(template.OrganizationId);
    }

    [Fact]
    public void Notification_Create_WithEmptyTitle_ShouldThrow()
    {
        var act = () => Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  ",
            "Mensagem",
            NotificationType.MissionCreated,
            DateTime.UtcNow);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void CollaboratorAccessLog_Create_WithEmptyCollaborator_ShouldThrow()
    {
        var act = () => CollaboratorAccessLog.Create(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            DateTime.UtcNow);

        act.Should().Throw<DomainInvariantException>();
    }
}
