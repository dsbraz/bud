using System.Reflection;
using Bud.Server.Controllers;
using Bud.Server.Data;
using Bud.Server.DependencyInjection;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;

namespace Bud.Server.Tests.Architecture;

public sealed class ArchitectureTests
{
    private static readonly Assembly ServerAssembly = typeof(Program).Assembly;
    private static readonly Assembly SharedAssembly = typeof(ITenantEntity).Assembly;
    private const string ValueObjectGuardrailsRelativePath = "docs/architecture/value-object-mapping-guardrails.md";

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

    [Fact]
    public void DbContextEntities_ShouldHaveEntityTypeConfigurationClass()
    {
        var entityTypes = typeof(ApplicationDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToList();

        entityTypes.Should().NotBeEmpty();

        var configurationTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
            .ToList();

        var missingConfiguration = entityTypes
            .Where(entityType =>
            {
                var expectedInterface = typeof(IEntityTypeConfiguration<>).MakeGenericType(entityType);
                return !configurationTypes.Any(configType => expectedInterface.IsAssignableFrom(configType));
            })
            .Select(t => t.FullName)
            .ToList();

        missingConfiguration.Should().BeEmpty("todas as entidades do DbContext devem ter IConfiguration dedicada");
    }

    [Fact]
    public void TenantEntities_MustHaveQueryFilter()
    {
        var tenantEntityTypes = SharedAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(ITenantEntity)))
            .ToList();

        tenantEntityTypes.Should().NotBeEmpty();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var missingFilter = tenantEntityTypes
            .Where(type =>
            {
                var entityType = context.Model.FindEntityType(type);
                return entityType is null || entityType.GetDeclaredQueryFilters().Count == 0;
            })
            .Select(t => t.FullName)
            .ToList();

        missingFilter.Should().BeEmpty(
            "todas as entidades que implementam ITenantEntity devem ter HasQueryFilter configurado para isolamento de tenant");
    }

    [Fact]
    public void Controllers_ExceptAuth_MustHaveClassLevelAuthorizeAttribute()
    {
        var controllerTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(ControllerBase)))
            .Where(t => t != typeof(AuthController))
            .ToList();

        controllerTypes.Should().NotBeEmpty();

        var unprotected = controllerTypes
            .Where(t => !t.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any())
            .Select(t => t.FullName)
            .ToList();

        unprotected.Should().BeEmpty(
            "todos os controllers (exceto AuthController) devem ter [Authorize] no nível de classe");
    }

    [Fact]
    public void Controllers_MustInheritFromApiControllerBase()
    {
        var controllerTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(ControllerBase)))
            .ToList();

        controllerTypes.Should().NotBeEmpty();

        var nonCompliant = controllerTypes
            .Where(t => !t.IsAssignableTo(typeof(ApiControllerBase)))
            .Select(t => t.FullName)
            .ToList();

        nonCompliant.Should().BeEmpty(
            "todos os controllers devem herdar de ApiControllerBase para mapeamento centralizado de ServiceResult");
    }

    [Fact]
    public void AggregateRoots_ShouldExposeRequiredDomainBehaviorMethods()
    {
        var requiredMethodsByType = new Dictionary<Type, string[]>
        {
            [typeof(Organization)] = ["Create", "Rename", "AssignOwner"],
            [typeof(Workspace)] = ["Create", "Rename"],
            [typeof(Team)] = ["Create", "Rename", "Reparent"],
            [typeof(Collaborator)] = ["Create", "UpdateProfile"],
            [typeof(Mission)] = ["Create", "UpdateDetails", "SetScope"],
            [typeof(MissionTemplate)] = ["Create", "UpdateBasics"]
        };

        foreach (var (type, requiredMethods) in requiredMethodsByType)
        {
            var methodNames = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .ToHashSet(StringComparer.Ordinal);

            methodNames.Should().Contain(requiredMethods,
                $"{type.Name} deve expor comportamento de domínio explícito para evitar modelo anêmico");
        }
    }

    [Fact]
    public void Services_ShouldMapCriticalRequestFieldsThroughValueObjects()
    {
        var repositoryRoot = FindRepositoryRoot();
        var guardrails = LoadValueObjectGuardrails(repositoryRoot);
        guardrails.Should().NotBeEmpty("deve existir ao menos um guardrail declarativo para mapeamento de Value Objects");

        foreach (var guardrail in guardrails)
        {
            AssertSourceContains(repositoryRoot, guardrail.Path, guardrail.Required, guardrail.Forbidden);
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var hasAgentsFile = File.Exists(Path.Combine(directory.FullName, "AGENTS.md"));
            var hasServerProject = File.Exists(Path.Combine(directory.FullName, "src", "Bud.Server", "Bud.Server.csproj"));
            if (hasAgentsFile && hasServerProject)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Não foi possível localizar a raiz do repositório para carregar a allowlist de serviços compostos.");
    }

    private static void AssertSourceContains(
        string repositoryRoot,
        string relativePath,
        string[] required,
        string[] forbidden)
    {
        var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).Should().BeTrue($"arquivo esperado não encontrado: {relativePath}");

        var source = File.ReadAllText(fullPath);

        foreach (var requiredSnippet in required)
        {
            source.Should().Contain(requiredSnippet, $"{relativePath} deve conter mapeamento explícito para Value Object");
        }

        foreach (var forbiddenSnippet in forbidden)
        {
            source.Should().NotContain(forbiddenSnippet, $"{relativePath} não deve usar primitive diretamente em chamadas de domínio críticas");
        }
    }

    private static List<ValueObjectGuardrail> LoadValueObjectGuardrails(string repositoryRoot)
    {
        var filePath = Path.Combine(
            repositoryRoot,
            ValueObjectGuardrailsRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Arquivo de guardrails não encontrado: {filePath}");
        }

        var guardrails = new List<ValueObjectGuardrail>();

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            // Allow markdown prose in the guardrail file; only parse rule lines.
            if (!line.Contains('|') || !line.StartsWith("src/", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split('|', 3, StringSplitOptions.None);
            if (parts.Length != 3)
            {
                throw new InvalidOperationException($"Linha inválida no arquivo de guardrails: {line}");
            }

            var path = parts[0].Trim();
            var required = SplitSnippets(parts[1]);
            var forbidden = SplitSnippets(parts[2]);

            guardrails.Add(new ValueObjectGuardrail(path, required, forbidden));
        }

        return guardrails;
    }

    private static string[] SplitSnippets(string section)
        => section
            .Split("||", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

    private sealed record ValueObjectGuardrail(string Path, string[] Required, string[] Forbidden);
}
