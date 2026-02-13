using System.Security.Claims;
using System.Text.Json;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Bud.Server.Tests.MultiTenancy;

public sealed class TenantRequiredMiddlewareTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static DefaultHttpContext CreateHttpContext(string path, bool isAuthenticated = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (isAuthenticated)
        {
            var claims = new[] { new Claim(ClaimTypes.Email, "user@example.com") };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }
        else
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return context;
    }

    private static async Task<ProblemDetails?> ReadProblemDetailsFromResponse(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);
    }

    [Theory]
    [InlineData("/swagger")]
    [InlineData("/health")]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/logout")]
    [InlineData("/api/auth/my-organizations")]
    public async Task InvokeAsync_NonApiOrExcludedPath_CallsNext(string path)
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateHttpContext(path);
        var tenantProvider = new Mock<ITenantProvider>();
        var tenantAuth = new Mock<ITenantAuthorizationService>();

        // Act
        await middleware.InvokeAsync(context, tenantProvider.Object, tenantAuth.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ApiPathWithoutAuthentication_Returns401()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateHttpContext("/api/organizations", isAuthenticated: false);
        var tenantProvider = new Mock<ITenantProvider>();
        var tenantAuth = new Mock<ITenantAuthorizationService>();

        // Act
        await middleware.InvokeAsync(context, tenantProvider.Object, tenantAuth.Object);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var problem = await ReadProblemDetailsFromResponse(context);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problem.Title.Should().Be("Não autenticado");
        problem.Detail.Should().Be("É necessário autenticação para acessar este recurso.");
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedWithoutTenantAndNotGlobalAdmin_Returns403()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateHttpContext("/api/organizations", isAuthenticated: true);
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.TenantId).Returns((Guid?)null);

        var tenantAuth = new Mock<ITenantAuthorizationService>();

        // Act
        await middleware.InvokeAsync(context, tenantProvider.Object, tenantAuth.Object);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var problem = await ReadProblemDetailsFromResponse(context);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status403Forbidden);
        problem.Title.Should().Be("Acesso negado");
        problem.Detail.Should().Be("É necessário selecionar uma organização para acessar este recurso.");
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedWithInvalidTenant_Returns403()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var tenantId = Guid.NewGuid();
        var context = CreateHttpContext("/api/organizations", isAuthenticated: true);
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);

        var tenantAuth = new Mock<ITenantAuthorizationService>();
        tenantAuth
            .Setup(t => t.UserBelongsToTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await middleware.InvokeAsync(context, tenantProvider.Object, tenantAuth.Object);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var problem = await ReadProblemDetailsFromResponse(context);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status403Forbidden);
        problem.Title.Should().Be("Acesso negado");
        problem.Detail.Should().Be("Você não tem permissão para acessar esta organização.");
    }

    [Fact]
    public async Task InvokeAsync_GlobalAdminWithoutTenant_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateHttpContext("/api/organizations", isAuthenticated: true);
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(true);
        tenantProvider.SetupGet(t => t.TenantId).Returns((Guid?)null);

        var tenantAuth = new Mock<ITenantAuthorizationService>();

        // Act
        await middleware.InvokeAsync(context, tenantProvider.Object, tenantAuth.Object);

        // Assert
        nextCalled.Should().BeTrue();
        tenantAuth.Verify(
            t => t.UserBelongsToTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_GlobalAdminWithTenant_CallsNextWithoutValidation()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var tenantId = Guid.NewGuid();
        var context = CreateHttpContext("/api/organizations", isAuthenticated: true);
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(true);
        tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);

        var tenantAuth = new Mock<ITenantAuthorizationService>();

        // Act
        await middleware.InvokeAsync(context, tenantProvider.Object, tenantAuth.Object);

        // Assert
        nextCalled.Should().BeTrue();
        tenantAuth.Verify(
            t => t.UserBelongsToTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedWithValidTenant_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var tenantId = Guid.NewGuid();
        var context = CreateHttpContext("/api/organizations", isAuthenticated: true);
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.IsGlobalAdmin).Returns(false);
        tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);

        var tenantAuth = new Mock<ITenantAuthorizationService>();
        tenantAuth
            .Setup(t => t.UserBelongsToTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await middleware.InvokeAsync(context, tenantProvider.Object, tenantAuth.Object);

        // Assert
        nextCalled.Should().BeTrue();
        tenantAuth.Verify(
            t => t.UserBelongsToTenantAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
