using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Events;
using Bud.Server.Domain.Model;
using Bud.Server.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Infrastructure.Persistence;

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
        var mission = Mission.Create(
            Guid.NewGuid(),
            organizationId,
            "Missão com evento",
            "Descrição",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            MissionStatus.Planned);

        dbContext.Missions.Add(mission);
        var unitOfWork = new EfUnitOfWork(dbContext, dispatcherMock.Object);

        await unitOfWork.CommitAsync(CancellationToken.None);

        dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IReadOnlyCollection<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        dispatchedEvents.Should().NotBeNull();
        var dispatchedEvent = dispatchedEvents!.Should().ContainSingle().Subject;
        var created = dispatchedEvent.Should().BeOfType<MissionCreatedDomainEvent>().Subject;
        created.MissionId.Should().Be(mission.Id);
        created.OrganizationId.Should().Be(organizationId);
        mission.DomainEvents.Should().BeEmpty();
    }
}
