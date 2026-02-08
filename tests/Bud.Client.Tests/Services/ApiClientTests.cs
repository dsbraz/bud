using System.Net;
using System.Text;
using Bud.Client.Services;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class ApiClientTests
{
    [Fact]
    public async Task GetOrganizationsAsync_WhenPageSizeExceedsServerLimit_ClampsTo100()
    {
        var handler = new CapturingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"items":[],"total":0,"page":1,"pageSize":100}
                    """, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new ApiClient(httpClient);

        _ = await client.GetOrganizationsAsync(search: null, page: 1, pageSize: 200);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/api/organizations?page=1&pageSize=100");
    }

    [Fact]
    public async Task GetOrganizationsAsync_WhenPageIsInvalid_NormalizesTo1()
    {
        var handler = new CapturingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"items":[],"total":0,"page":1,"pageSize":10}
                    """, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new ApiClient(httpClient);

        _ = await client.GetOrganizationsAsync(search: null, page: 0, pageSize: 10);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/api/organizations?page=1&pageSize=10");
    }

    [Fact]
    public async Task GetMissionsAsync_WhenPageSizeExceedsLimit_ClampsTo100()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(handler);

        _ = await client.GetMissionsAsync(MissionScopeType.Organization, Guid.NewGuid(), null, 1, 200);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.Query.Should().Contain("pageSize=100");
    }

    [Fact]
    public async Task GetCollaboratorsAsync_WhenPageSizeExceedsLimit_ClampsTo100()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(handler);

        _ = await client.GetCollaboratorsAsync(null, null, 1, 1000);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/api/collaborators?page=1&pageSize=100");
    }

    [Fact]
    public async Task GetMyThingsAsync_WhenPageAndPageSizeAreInvalid_NormalizesBoth()
    {
        var collaboratorId = Guid.NewGuid();
        var handler = CreateSuccessHandler();
        var client = CreateClient(handler);

        _ = await client.GetMyThingsAsync(collaboratorId, null, 0, 200);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/api/missions/my-missions/{collaboratorId}?search=&page=1&pageSize=100");
    }

    [Fact]
    public async Task GetMetricCheckinsAsync_WhenPageSizeExceedsLimit_ClampsTo100()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(handler);

        _ = await client.GetMetricCheckinsAsync(null, null, 1, 1000);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/api/metric-checkins?page=1&pageSize=100");
    }

    private static CapturingHandler CreateSuccessHandler()
        => new(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"items":[],"total":0,"page":1,"pageSize":100}
                    """, Encoding.UTF8, "application/json")
            });

    private static ApiClient CreateClient(CapturingHandler handler)
        => new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });
    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}
