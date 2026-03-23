using Bud.Infrastructure.Persistence;
using Bud.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Infrastructure.UnitTests.Infrastructure.Repositories;

public sealed class NotificationRepositoryTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<(Organization org, Collaborator collaborator)> CreateTestRecipient(
        ApplicationDbContext context)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test Recipient",
            Email = "recipient@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        return (org, collaborator);
    }

    private static Notification CreateTestNotification(
        Guid recipientId,
        Guid organizationId,
        bool isRead = false,
        DateTime? createdAtUtc = null,
        string title = "Test Title",
        string message = "Test message content")
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientCollaboratorId = recipientId,
            OrganizationId = organizationId,
            Title = title,
            Message = message,
            Type = NotificationType.GoalCreated,
            IsRead = isRead,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            ReadAtUtc = isRead ? DateTime.UtcNow : null
        };
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenNotificationExists_ReturnsNotification()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);
        var (org, collaborator) = await CreateTestRecipient(context);

        var notification = CreateTestNotification(collaborator.Id, org.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(notification.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
        result.Title.Should().Be("Test Title");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotificationNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByRecipientAsync Tests

    [Fact]
    public async Task GetByRecipientAsync_FiltersByRecipientId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);
        var (org, collaborator1) = await CreateTestRecipient(context);

        var collaborator2 = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Other Recipient",
            Email = "other@test.com",
            OrganizationId = org.Id
        };
        context.Collaborators.Add(collaborator2);
        await context.SaveChangesAsync();

        context.Notifications.AddRange(
            CreateTestNotification(collaborator1.Id, org.Id, title: "For Recipient 1"),
            CreateTestNotification(collaborator2.Id, org.Id, title: "For Recipient 2"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByRecipientAsync(collaborator1.Id, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("For Recipient 1");
    }

    [Fact]
    public async Task GetByRecipientAsync_FiltersByIsReadTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);
        var (org, collaborator) = await CreateTestRecipient(context);

        context.Notifications.AddRange(
            CreateTestNotification(collaborator.Id, org.Id, isRead: true, title: "Read"),
            CreateTestNotification(collaborator.Id, org.Id, isRead: false, title: "Unread"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByRecipientAsync(collaborator.Id, true, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Read");
    }

    [Fact]
    public async Task GetByRecipientAsync_FiltersByIsReadFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);
        var (org, collaborator) = await CreateTestRecipient(context);

        context.Notifications.AddRange(
            CreateTestNotification(collaborator.Id, org.Id, isRead: true, title: "Read"),
            CreateTestNotification(collaborator.Id, org.Id, isRead: false, title: "Unread"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByRecipientAsync(collaborator.Id, false, 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Unread");
    }

    [Fact]
    public async Task GetByRecipientAsync_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);
        var (org, collaborator) = await CreateTestRecipient(context);

        var date1 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var date3 = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        context.Notifications.AddRange(
            CreateTestNotification(collaborator.Id, org.Id, createdAtUtc: date2, title: "Middle"),
            CreateTestNotification(collaborator.Id, org.Id, createdAtUtc: date1, title: "Oldest"),
            CreateTestNotification(collaborator.Id, org.Id, createdAtUtc: date3, title: "Newest"));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByRecipientAsync(collaborator.Id, null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].CreatedAtUtc.Should().Be(date3);
        result.Items[1].CreatedAtUtc.Should().Be(date2);
        result.Items[2].CreatedAtUtc.Should().Be(date1);
    }

    [Fact]
    public async Task GetByRecipientAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);
        var (org, collaborator) = await CreateTestRecipient(context);

        for (int i = 0; i < 5; i++)
        {
            context.Notifications.Add(
                CreateTestNotification(
                    collaborator.Id,
                    org.Id,
                    createdAtUtc: DateTime.UtcNow.AddMinutes(i),
                    title: $"Notification {i:D2}"));
        }
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByRecipientAsync(collaborator.Id, null, 1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    #region AddRangeAsync / SaveChangesAsync Tests

    [Fact]
    public async Task AddRangeAsync_PersistsMultipleNotifications()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new NotificationRepository(context);
        var (org, collaborator) = await CreateTestRecipient(context);

        var notifications = new List<Notification>
        {
            CreateTestNotification(collaborator.Id, org.Id, title: "Notification 1"),
            CreateTestNotification(collaborator.Id, org.Id, title: "Notification 2"),
            CreateTestNotification(collaborator.Id, org.Id, title: "Notification 3")
        };

        // Act
        await repository.AddRangeAsync(notifications);
        await repository.SaveChangesAsync();

        // Assert
        var count = await context.Notifications
            .Where(n => n.RecipientCollaboratorId == collaborator.Id)
            .CountAsync();
        count.Should().Be(3);
    }

    #endregion
}
