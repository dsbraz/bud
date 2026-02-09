using System.Reflection;
using Bud.Server.Controllers;
using Bud.Server.Data;
using Bud.Server.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Bud.Server.Tests.Architecture;

public sealed class ArchitectureTests
{
    private static readonly Assembly ServerAssembly = typeof(Program).Assembly;

    [Fact]
    public void Controllers_ShouldNotDependOnApplicationDbContext()
    {
        var controllerTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(ControllerBase)))
            .ToList();

        controllerTypes.Should().NotBeEmpty();

        var invalidControllers = controllerTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(ApplicationDbContext)))
            .Select(t => t.FullName)
            .ToList();

        invalidControllers.Should().BeEmpty("controllers não devem depender diretamente do DbContext");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOnControllersNamespace()
    {
        var applicationTypes = ServerAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true)
            .ToList();

        applicationTypes.Should().NotBeEmpty();

        var invalidTypes = applicationTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType.Namespace?.StartsWith("Bud.Server.Controllers", StringComparison.Ordinal) == true))
            .Select(t => t.FullName)
            .ToList();

        invalidTypes.Should().BeEmpty("camada Application não deve depender da camada Controllers");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOnApplicationDbContextInConstructors()
    {
        var applicationTypes = ServerAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true)
            .ToList();

        applicationTypes.Should().NotBeEmpty();

        var invalidTypes = applicationTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(ApplicationDbContext)))
            .Select(t => t.FullName)
            .ToList();

        invalidTypes.Should().BeEmpty("camada Application deve depender de abstrações e não do DbContext");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOnAspNetAuthorizationInConstructors()
    {
        var applicationTypes = ServerAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true)
            .ToList();

        applicationTypes.Should().NotBeEmpty();

        var invalidTypes = applicationTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(IAuthorizationService)))
            .Select(t => t.FullName)
            .ToList();

        invalidTypes.Should().BeEmpty("camada Application deve depender de um gateway de autorização próprio");
    }

    [Fact]
    public void ServicesLayer_ShouldNotDependOnControllersNamespace()
    {
        var serviceTypes = ServerAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Bud.Server.Services", StringComparison.Ordinal) == true && t.IsClass && !t.IsAbstract)
            .ToList();

        serviceTypes.Should().NotBeEmpty();

        var invalidTypes = serviceTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType.Namespace?.StartsWith("Bud.Server.Controllers", StringComparison.Ordinal) == true))
            .Select(t => t.FullName)
            .ToList();

        invalidTypes.Should().BeEmpty("camada Services não deve depender de Controllers");
    }

    [Fact]
    public void Controllers_ShouldNotDependOnServicesContractsInConstructors()
    {
        var controllerTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(ControllerBase)))
            .ToList();

        controllerTypes.Should().NotBeEmpty();

        var invalidControllers = controllerTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Any(IsServicesContract))
            .Select(t => t.FullName)
            .ToList();

        invalidControllers.Should().BeEmpty("controllers devem depender de use cases e não de contratos legados da camada Services");
    }

    [Fact]
    public void UseCases_ShouldNotDependOnServicesContractsInConstructors()
    {
        var useCaseTypes = ServerAssembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true &&
                t.Name.EndsWith("UseCase", StringComparison.Ordinal))
            .ToList();

        useCaseTypes.Should().NotBeEmpty();

        var invalidUseCases = useCaseTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(IsServicesContract))
            .Select(t => t.FullName)
            .ToList();

        invalidUseCases.Should().BeEmpty("use cases devem depender de portas da Application, não de contratos legados em Services");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotExposeServicesNamespaceTypes()
    {
        var applicationTypes = ServerAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true)
            .ToList();

        applicationTypes.Should().NotBeEmpty();

        var invalidTypes = applicationTypes
            .Where(type =>
                UsesServicesNamespace(type) ||
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Any(method =>
                        UsesServicesNamespace(method.ReturnType) ||
                        method.GetParameters().Any(p => UsesServicesNamespace(p.ParameterType))))
            .Select(t => t.FullName)
            .ToList();

        invalidTypes.Should().BeEmpty("camada Application não deve expor tipos do namespace Bud.Server.Services");
    }

    [Fact]
    public void DependencyInjection_ShouldExposeModularCompositionExtensions()
    {
        typeof(BudApiCompositionExtensions).Should().NotBeNull();
        typeof(BudSecurityCompositionExtensions).Should().NotBeNull();
        typeof(BudDataCompositionExtensions).Should().NotBeNull();
        typeof(BudApplicationCompositionExtensions).Should().NotBeNull();
        typeof(BudCompositionExtensions).Should().NotBeNull();
    }

    [Fact]
    public void DependencyInjection_ShouldExposeRequiredExtensionMethods()
    {
        var extensionMethods = new[]
        {
            (Type: typeof(BudApiCompositionExtensions), Method: "AddBudApi"),
            (Type: typeof(BudApiCompositionExtensions), Method: "AddBudSettings"),
            (Type: typeof(BudSecurityCompositionExtensions), Method: "AddBudAuthentication"),
            (Type: typeof(BudSecurityCompositionExtensions), Method: "AddBudAuthorization"),
            (Type: typeof(BudDataCompositionExtensions), Method: "AddBudDataAccess"),
            (Type: typeof(BudApplicationCompositionExtensions), Method: "AddBudApplication"),
            (Type: typeof(BudCompositionExtensions), Method: "AddBudPlatform")
        };

        foreach (var extension in extensionMethods)
        {
            extension.Type
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(m => m.Name)
                .Should()
                .Contain(extension.Method, $"a extensão {extension.Type.Name} deve expor {extension.Method}");
        }
    }

    private static bool IsServicesContract(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        if (!parameterType.IsInterface)
        {
            return false;
        }

        return parameterType.Namespace?.StartsWith("Bud.Server.Services", StringComparison.Ordinal) == true
            && parameterType.Name.EndsWith("Service", StringComparison.Ordinal);
    }

    private static bool UsesServicesNamespace(Type type)
        => type.Namespace?.StartsWith("Bud.Server.Services", StringComparison.Ordinal) == true;
}
