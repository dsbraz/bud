using Bud.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bud.Infrastructure.UnitTests.Infrastructure.Persistence;

public sealed class EfUnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_WhenEntitiesHaveDomainEvents_DispatchesAndClearsEvents()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        IReadOnlyCollection<IDomainEvent>? dispatchedEvents = null;
        dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<IReadOnlyCollection<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents = events)
            .Returns(Task.CompletedTask);

        var organizationId = Guid.NewGuid();
        var mission = Goal.Create(
            Guid.NewGuid(),
            organizationId,
            "Missão com evento",
            "Descrição",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            GoalStatus.Planned);

        dbContext.Goals.Add(mission);
        var unitOfWork = new EfUnitOfWork(dbContext, dispatcherMock.Object);

        await unitOfWork.CommitAsync(CancellationToken.None);

        dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IReadOnlyCollection<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        dispatchedEvents.Should().NotBeNull();
        var dispatchedEvent = dispatchedEvents!.Should().ContainSingle().Subject;
        var created = dispatchedEvent.Should().BeOfType<GoalCreatedDomainEvent>().Subject;
        created.GoalId.Should().Be(mission.Id);
        created.OrganizationId.Should().Be(organizationId);
        mission.DomainEvents.Should().BeEmpty();
    }
}
