using System.Net;
using System.Net.Http.Json;
using Bud.Server.Data;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class OutboxEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OutboxEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateGlobalAdminClient();
    }

    [Fact]
    public async Task GetDeadLetters_ReturnsOnlyDeadLetters()
    {
        var deadLetterId = await AddMessageAsync(deadLettered: true);
        await AddMessageAsync(deadLettered: false);

        var response = await _client.GetAsync("/api/outbox/dead-letters?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OutboxDeadLetterDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle(i => i.Id == deadLetterId);
        result.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ReprocessDeadLetter_WhenExists_ReturnsNoContent_AndResetsState()
    {
        var deadLetterId = await AddMessageAsync(deadLettered: true);

        var response = await _client.PostAsync($"/api/outbox/dead-letters/{deadLetterId}/reprocess", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var message = await db.OutboxMessages.IgnoreQueryFilters().SingleAsync(m => m.Id == deadLetterId);
        message.DeadLetteredOnUtc.Should().BeNull();
        message.RetryCount.Should().Be(0);
        message.NextAttemptOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ReprocessDeadLetter_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/outbox/dead-letters/{Guid.NewGuid()}/reprocess", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("Mensagem de outbox não encontrada.");
    }

    [Fact]
    public async Task ReprocessDeadLettersBatch_WithFilters_ReturnsCount()
    {
        await AddMessageAsync(deadLettered: true, eventType: "mission.updated");
        await AddMessageAsync(deadLettered: true, eventType: "team.updated");

        var response = await _client.PostAsJsonAsync("/api/outbox/dead-letters/reprocess", new ReprocessDeadLettersRequest
        {
            EventType = "mission",
            MaxItems = 100
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReprocessDeadLettersResponse>();
        payload.Should().NotBeNull();
        payload!.ReprocessedCount.Should().Be(1);
    }

    [Fact]
    public async Task ReprocessDeadLettersBatch_WithInvalidMaxItems_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/outbox/dead-letters/reprocess", new ReprocessDeadLettersRequest
        {
            MaxItems = 1000
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("O parâmetro 'maxItems' deve estar entre 1 e 500.");
    }

    private async Task<Guid> AddMessageAsync(bool deadLettered, string eventType = "test-event")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var message = new OutboxMessage
        {
            EventType = eventType,
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow.AddMinutes(-5),
            RetryCount = deadLettered ? 5 : 0,
            DeadLetteredOnUtc = deadLettered ? DateTime.UtcNow.AddMinutes(-1) : null,
            NextAttemptOnUtc = deadLettered ? null : DateTime.UtcNow,
            Error = deadLettered ? "erro" : null
        };

        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
        return message.Id;
    }
}
