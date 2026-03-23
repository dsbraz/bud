using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bud.Api.Controllers;
using Bud.Api.DependencyInjection;
using Bud.Application;
using Bud.Infrastructure;
using Bud.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Bud.ArchitectureTests.Architecture;

public sealed class ArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;
    private static readonly Regex NamespaceRegex = new(@"^\s*namespace\s+(?<namespace>[A-Za-z0-9_.]+)\s*[;{]", RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void Domain_should_not_reference_other_solution_projects()
    {
        var references = GetProjectReferenceNames("src/Api/Bud.Domain/Bud.Domain.csproj");

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
        var references = GetProjectReferenceNames("src/Api/Bud.Application/Bud.Application.csproj");

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
        var references = GetProjectReferenceNames("src/Api/Bud.Infrastructure/Bud.Infrastructure.csproj");

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
        var references = GetProjectReferenceNames("src/Api/Bud.Api/Bud.Api.csproj");

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
    public void Application_repositories_should_not_depend_on_http_response_contracts()
    {
        var repositoryRoot = TestRepositoryRoot.Find();
        var repositoryFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "Api", "Bud.Application"), "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).StartsWith('I') && Path.GetFileName(path).EndsWith("Repository.cs", StringComparison.Ordinal))
            .ToList();

        repositoryFiles.Should().NotBeEmpty();

        var invalidFiles = repositoryFiles
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("Bud.Shared.Contracts.Responses", StringComparison.Ordinal)
                    || source.Contains("Bud.Shared.Contracts.Common", StringComparison.Ordinal)
                    || source.Contains("Bud.Infrastructure", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToList();

        invalidFiles.Should().BeEmpty();
    }

    [Fact]
    public void Value_object_guardrails_document_should_point_to_new_application_paths()
    {
        var repositoryRoot = TestRepositoryRoot.Find();
        var filePath = Path.Combine(repositoryRoot, "docs", "architecture", "value-object-mapping-guardrails.md");

        File.Exists(filePath).Should().BeTrue();

        var content = File.ReadAllText(filePath);
        content.Should().Contain("src/Api/Bud.Application/");
        content.Should().NotContain("src/Bud.Server/");
    }

    [Fact]
    public void Explicit_namespaces_should_match_project_root_namespace_and_folder_structure()
    {
        var repositoryRoot = TestRepositoryRoot.Find();
        var projectFiles = Directory
            .EnumerateFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        projectFiles.Should().NotBeEmpty();

        var mismatches = new List<string>();

        foreach (var projectFile in projectFiles)
        {
            var projectDirectory = Path.GetDirectoryName(projectFile)!;
            var document = XDocument.Load(projectFile);
            var rootNamespace = document.Descendants("RootNamespace").Select(static element => element.Value).SingleOrDefault();
            var assemblyName = document.Descendants("AssemblyName").Select(static element => element.Value).SingleOrDefault();
            var baseNamespace = !string.IsNullOrWhiteSpace(rootNamespace)
                ? rootNamespace
                : !string.IsNullOrWhiteSpace(assemblyName)
                    ? assemblyName
                    : Path.GetFileNameWithoutExtension(projectFile);

            foreach (var sourceFile in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (sourceFile.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || sourceFile.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var content = File.ReadAllText(sourceFile);
                var match = NamespaceRegex.Match(content);
                if (!match.Success)
                {
                    continue;
                }

                var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, sourceFile));
                var expectedNamespace = string.IsNullOrEmpty(relativeDirectory)
                    ? baseNamespace
                    : $"{baseNamespace}.{relativeDirectory.Replace(Path.DirectorySeparatorChar, '.')}";
                var declaredNamespace = match.Groups["namespace"].Value;

                if (!string.Equals(declaredNamespace, expectedNamespace, StringComparison.Ordinal))
                {
                    mismatches.Add($"{Path.GetRelativePath(repositoryRoot, sourceFile)} => {declaredNamespace} (esperado: {expectedNamespace})");
                }
            }
        }

        mismatches.Should().BeEmpty();
    }

    private static HashSet<string> GetProjectReferenceNames(string relativeProjectPath)
    {
        var projectPath = Path.Combine(TestRepositoryRoot.Find(), relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
            .Select(static value => Path.GetFileNameWithoutExtension(value))
            .ToHashSet(StringComparer.Ordinal);
    }
}
