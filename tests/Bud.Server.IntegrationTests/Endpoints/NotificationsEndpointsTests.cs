using System.Net;
using System.Net.Http.Json;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class NotificationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid orgId, Guid collaboratorId, HttpClient client)> SetupTenantUser()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var org = await dbContext.Organizations.IgnoreQueryFilters().FirstAsync();

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Notification User",
            Email = $"notif-{Guid.NewGuid()}@example.com",
            OrganizationId = org.Id,
            Role = Bud.Server.Domain.Model.CollaboratorRole.IndividualContributor
        };
        dbContext.Collaborators.Add(collaborator);
        await dbContext.SaveChangesAsync();

        var client = _factory.CreateTenantClient(org.Id, collaborator.Email, collaborator.Id);
        return (org.Id, collaborator.Id, client);
    }

    private async Task SeedNotifications(Guid orgId, Guid collaboratorId, int count, bool read = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (var i = 0; i < count; i++)
        {
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientCollaboratorId = collaboratorId,
                OrganizationId = orgId,
                Title = $"Notification {i}",
                Message = $"Message {i}",
                Type = NotificationType.MissionCreated,
                IsRead = read,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                ReadAtUtc = read ? DateTime.UtcNow : null,
                RelatedEntityId = Guid.NewGuid(),
                RelatedEntityType = "Mission"
            });
        }
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAll_WithNotifications_ReturnsPagedResult()
    {
        // Arrange
        var (orgId, collaboratorId, client) = await SetupTenantUser();
        await SeedNotifications(orgId, collaboratorId, 5);

        // Act
        var response = await client.GetAsync("/api/notifications?page=1&pageSize=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_WithIsReadFalseFilter_ReturnsOnlyUnread()
    {
        // Arrange
        var (orgId, collaboratorId, client) = await SetupTenantUser();
        await SeedNotifications(orgId, collaboratorId, 3, read: false);
        await SeedNotifications(orgId, collaboratorId, 2, read: true);

        // Act
        var response = await client.GetAsync("/api/notifications?isRead=false&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(3);
        result.Items.Should().OnlyContain(n => !n.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_WithValidNotification_ReturnsNoContent()
    {
        // Arrange
        var (orgId, collaboratorId, client) = await SetupTenantUser();

        Guid notificationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientCollaboratorId = collaboratorId,
                OrganizationId = orgId,
                Title = "To Read",
                Message = "To be read",
                Type = NotificationType.MissionCreated,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync();
            notificationId = notification.Id;
        }

        // Act
        var response = await client.PatchAsync($"/api/notifications/{notificationId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await verifyDb.Notifications.IgnoreQueryFilters().FirstAsync(n => n.Id == notificationId);
        updated.IsRead.Should().BeTrue();
        updated.ReadAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllUnread()
    {
        // Arrange
        var (orgId, collaboratorId, client) = await SetupTenantUser();
        await SeedNotifications(orgId, collaboratorId, 3, read: false);

        // Act
        var response = await client.PatchAsync("/api/notifications", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify unread count is 0
        var listResponse = await client.GetAsync("/api/notifications?isRead=false&page=1&pageSize=10");
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        result!.Total.Should().Be(0);
    }

    [Fact]
    public async Task MarkAsRead_NonExistentNotification_ReturnsNotFound()
    {
        // Arrange
        var (_, _, client) = await SetupTenantUser();

        // Act
        var response = await client.PatchAsync($"/api/notifications/{Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithoutTenant_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateUserClientWithoutTenant("no-tenant@example.com");

        // Act
        var response = await client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
