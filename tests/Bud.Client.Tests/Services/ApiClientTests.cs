using System.Net;
using System.Text;
using Bud.Client.Services;
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
