using FluentAssertions;
using Xunit;

namespace Bud.Domain.UnitTests.Domain.Models;

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
        var team = Team.Create(id, Guid.NewGuid(), Guid.NewGuid(), "Team", Guid.NewGuid());

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
    public void Collaborator_EnsureCanOwnOrganization_WithNonLeaderRole_ShouldThrow()
    {
        var collaborator = Collaborator.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ana",
            "ana@getbud.co",
            CollaboratorRole.IndividualContributor);

        var act = () => collaborator.EnsureCanOwnOrganization();

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Collaborator_EnsureCanOwnOrganization_WithLeaderRole_ShouldNotThrow()
    {
        var collaborator = Collaborator.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ana",
            "ana@getbud.co",
            CollaboratorRole.Leader);

        var act = () => collaborator.EnsureCanOwnOrganization();

        act.Should().NotThrow();
    }

    [Fact]
    public void Goal_UpdateDetails_WithInvalidDateRange_ShouldThrow()
    {
        var goal = Goal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Meta",
            null,
            null,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            GoalStatus.Active);

        var act = () => goal.UpdateDetails(
            "Meta",
            null,
            null,
            new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            GoalStatus.Active);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Goal_UpdateDetails_WithNameLongerThan200_ShouldThrow()
    {
        var goal = Goal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Meta",
            null,
            null,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            GoalStatus.Active);

        var longName = new string('A', 201);
        var act = () => goal.UpdateDetails(
            longName,
            null,
            null,
            new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 13, 0, 0, 0, DateTimeKind.Utc),
            GoalStatus.Active);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void GoalIndicator_ApplyTarget_WithInvalidRange_ShouldThrow()
    {
        var indicator = Indicator.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Indicador", IndicatorType.Quantitative);

        var act = () => indicator.ApplyTarget(
            IndicatorType.Quantitative,
            QuantitativeIndicatorType.KeepBetween,
            100m,
            100m,
            IndicatorUnit.Percentage,
            null);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Checkin_Update_WithConfidenceOutOfRange_ShouldThrow()
    {
        var checkin = Checkin.Create(
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
    public void GoalIndicator_CreateCheckin_WithMissingQuantitativeValue_ShouldThrow()
    {
        var indicator = Indicator.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Indicador", IndicatorType.Quantitative);

        var act = () => indicator.CreateCheckin(
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
    public void GoalIndicator_UpdateCheckin_WithMissingQualitativeText_ShouldThrow()
    {
        var indicator = Indicator.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Indicador", IndicatorType.Qualitative);
        var checkin = Checkin.Create(
            Guid.NewGuid(),
            indicator.OrganizationId,
            indicator.Id,
            Guid.NewGuid(),
            null,
            "Texto",
            DateTime.UtcNow,
            null,
            3);

        var act = () => indicator.UpdateCheckin(checkin, null, "   ", DateTime.UtcNow, null, 3);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void TemplateIndicator_Create_WithQuantitativeTypeMissing_ShouldThrow()
    {
        var act = () => TemplateIndicator.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Receita",
            IndicatorType.Quantitative,
            0,
            null,
            null,
            0m,
            100m,
            IndicatorUnit.Percentage,
            null);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Template_ReplaceIndicators_ShouldSetTemplateAndOrganizationIds()
    {
        var template = Template.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Template",
            null,
            null,
            null);

        template.ReplaceIndicators(
        [
            new TemplateIndicatorDraft(
                "Qualidade",
                IndicatorType.Qualitative,
                0,
                null,
                null,
                null,
                null,
                null,
                "Meta textual")
        ]);

        template.Indicators.Should().ContainSingle();
        template.Indicators.First().TemplateId.Should().Be(template.Id);
        template.Indicators.First().OrganizationId.Should().Be(template.OrganizationId);
    }

    [Fact]
    public void Goal_Create_WithEmptyOrganization_ShouldThrow()
    {
        var act = () => Goal.Create(Guid.NewGuid(), Guid.Empty, "Meta", null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Goal_Create_WithEmptyName_ShouldThrow()
    {
        var act = () => Goal.Create(Guid.NewGuid(), Guid.NewGuid(), "  ", null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Goal_Create_WithNameLongerThan200_ShouldThrow()
    {
        var longName = new string('A', 201);
        var act = () => Goal.Create(Guid.NewGuid(), Guid.NewGuid(), longName, null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Goal_Create_WithValidData_ShouldSucceed()
    {
        var id = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var parent = Goal.Create(Guid.NewGuid(), orgId, "Meta pai", null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(30), GoalStatus.Planned);

        var goal = Goal.Create(id, orgId, "Meta", "Descrição", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned, parent.Id);

        goal.Id.Should().Be(id);
        goal.OrganizationId.Should().Be(orgId);
        goal.ParentId.Should().Be(parent.Id);
        goal.Name.Should().Be("Meta");
        goal.Description.Should().Be("Descrição");
    }

    [Fact]
    public void Goal_UpdateDetails_WithEmptyName_ShouldThrow()
    {
        var goal = Goal.Create(Guid.NewGuid(), Guid.NewGuid(), "Meta", null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        var act = () => goal.UpdateDetails("", null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        act.Should().Throw<DomainInvariantException>();
    }

    [Fact]
    public void Goal_UpdateDetails_TrimsDescription()
    {
        var goal = Goal.Create(Guid.NewGuid(), Guid.NewGuid(), "Meta", null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        goal.UpdateDetails("Novo Nome", "  Descrição  ", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        goal.Name.Should().Be("Novo Nome");
        goal.Description.Should().Be("Descrição");
    }

    [Fact]
    public void Goal_UpdateDetails_NullsEmptyDescription()
    {
        var goal = Goal.Create(Guid.NewGuid(), Guid.NewGuid(), "Meta", "Desc", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        goal.UpdateDetails("Nome", "   ", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        goal.Description.Should().BeNull();
    }

    [Fact]
    public void Goal_UpdateDetails_SetsDimension()
    {
        var goal = Goal.Create(Guid.NewGuid(), Guid.NewGuid(), "Meta", null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);
        var dimension = "Clientes";

        goal.UpdateDetails("Meta", null, dimension, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), GoalStatus.Planned);

        goal.Dimension.Should().Be(dimension);
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
            NotificationType.GoalCreated,
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

    [Fact]
    public void Goal_Create_WithParentId_SetsParentId()
    {
        var parentId = Guid.NewGuid();

        var child = Goal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Meta filha",
            null,
            null,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            GoalStatus.Planned,
            parentId);

        child.ParentId.Should().Be(parentId);
    }
}
