using System.Reflection;
using System.Xml.Linq;
using Bud.Api.Controllers;
using Bud.Api.DependencyInjection;
using Bud.Application;
using Bud.Domain.Model;
using Bud.Infrastructure;
using Bud.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Bud.ArchitectureTests.Architecture;

public sealed class ArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_should_not_reference_other_solution_projects()
    {
        var references = GetProjectReferenceNames("src/Server/Bud.Domain/Bud.Domain.csproj");

        references.Should().NotContain("Bud.Application");
        references.Should().NotContain("Bud.Infrastructure");
        references.Should().NotContain("Bud.Api");
        references.Should().NotContain("Bud.BlazorWasm");
        references.Should().Contain("Bud.Shared.Contracts");
        references.Should().Contain("Bud.Shared.Kernel");
    }

    [Fact]
    public void Application_should_reference_domain_and_shared_only()
    {
        var references = GetProjectReferenceNames("src/Server/Bud.Application/Bud.Application.csproj");

        references.Should().Contain("Bud.Domain");
        references.Should().Contain("Bud.Shared.Contracts");
        references.Should().Contain("Bud.Shared.Kernel");
        references.Should().NotContain("Bud.Infrastructure");
        references.Should().NotContain("Bud.Api");
        references.Should().NotContain("Bud.BlazorWasm");
    }

    [Fact]
    public void Infrastructure_should_reference_application_and_domain()
    {
        var references = GetProjectReferenceNames("src/Server/Bud.Infrastructure/Bud.Infrastructure.csproj");

        references.Should().Contain("Bud.Application");
        references.Should().Contain("Bud.Domain");
        references.Should().Contain("Bud.Shared.Contracts");
        references.Should().Contain("Bud.Shared.Kernel");
        references.Should().NotContain("Bud.Api");
        references.Should().NotContain("Bud.BlazorWasm");
    }

    [Fact]
    public void Api_should_reference_application_and_infrastructure()
    {
        var references = GetProjectReferenceNames("src/Server/Bud.Api/Bud.Api.csproj");

        references.Should().Contain("Bud.Application");
        references.Should().Contain("Bud.Infrastructure");
        references.Should().Contain("Bud.Shared.Contracts");
        references.Should().Contain("Bud.Shared.Kernel");
        references.Should().NotContain("Bud.BlazorWasm");
        references.Should().NotContain("Bud.Domain");
    }

    [Fact]
    public void DependencyInjection_should_expose_modular_extensions()
    {
        typeof(BudApiCompositionExtensions).Should().NotBeNull();
        typeof(BudSecurityCompositionExtensions).Should().NotBeNull();
        typeof(BudObservabilityCompositionExtensions).Should().NotBeNull();
        typeof(BudCompositionExtensions).Should().NotBeNull();
        typeof(BudApplicationCompositionExtensions).Should().NotBeNull();
        typeof(BudInfrastructureCompositionExtensions).Should().NotBeNull();
    }

    [Fact]
    public void Controllers_should_not_depend_on_application_db_context()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(ControllerBase)))
            .ToList();

        controllerTypes.Should().NotBeEmpty();

        var invalidControllers = controllerTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => parameter.ParameterType == typeof(ApplicationDbContext)))
            .Select(type => type.FullName)
            .ToList();

        invalidControllers.Should().BeEmpty();
    }

    [Fact]
    public void Controllers_must_inherit_from_api_controller_base()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(ControllerBase)))
            .ToList();

        controllerTypes.Should().NotBeEmpty();

        controllerTypes
            .Where(type => !type.IsAssignableTo(typeof(ApiControllerBase)))
            .Select(type => type.FullName)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void Domain_repositories_should_not_depend_on_application_read_models_or_http_responses()
    {
        var repositoryRoot = FindRepositoryRoot();
        var repositoryFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "Server", "Bud.Domain", "Repositories"), "*.cs", SearchOption.TopDirectoryOnly)
            .ToList();

        repositoryFiles.Should().NotBeEmpty();

        var invalidFiles = repositoryFiles
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("Bud.Application.ReadModels", StringComparison.Ordinal)
                    || source.Contains("Bud.Shared.Contracts.Responses", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToList();

        invalidFiles.Should().BeEmpty();
    }

    [Fact]
    public void Value_object_guardrails_document_should_point_to_new_application_paths()
    {
        var repositoryRoot = FindRepositoryRoot();
        var filePath = Path.Combine(repositoryRoot, "docs", "architecture", "value-object-mapping-guardrails.md");

        File.Exists(filePath).Should().BeTrue();

        var content = File.ReadAllText(filePath);
        content.Should().Contain("src/Server/Bud.Application/");
        content.Should().NotContain("src/Bud.Server/");
    }

    private static HashSet<string> GetProjectReferenceNames(string relativeProjectPath)
    {
        var projectPath = Path.Combine(FindRepositoryRoot(), relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
            .Select(static value => Path.GetFileNameWithoutExtension(value))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bud.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Não foi possível localizar a raiz do repositório.");
    }
}
