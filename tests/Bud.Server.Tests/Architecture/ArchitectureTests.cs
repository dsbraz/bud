using System.Reflection;
using Bud.Server.Application.Common.Events;
using Bud.Server.Controllers;
using Bud.Server.Data;
using Bud.Server.DependencyInjection;
using Bud.Server.Domain.Common.Events;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.Tests.Architecture;

public sealed class ArchitectureTests
{
    private static readonly Assembly ServerAssembly = typeof(Program).Assembly;
    private static readonly Assembly SharedAssembly = typeof(ITenantEntity).Assembly;
    private const string CompositeServiceAllowlistRelativePath = "docs/architecture/composite-services-allowlist.md";
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
    public void CommandUseCases_ShouldNotDependOnQueryServicesInConstructors()
    {
        var commandUseCases = ServerAssembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true &&
                t.Name.EndsWith("CommandUseCase", StringComparison.Ordinal))
            .ToList();

        commandUseCases.Should().NotBeEmpty();

        var invalidUseCases = commandUseCases
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(p =>
                    p.ParameterType.Namespace?.StartsWith("Bud.Server.Application.Abstractions", StringComparison.Ordinal) == true &&
                    p.ParameterType.Name.EndsWith("QueryService", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        invalidUseCases.Should().BeEmpty("command use cases não devem depender de portas query");
    }

    [Fact]
    public void QueryUseCases_ShouldNotDependOnCommandServicesInConstructors()
    {
        var queryUseCases = ServerAssembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true &&
                t.Name.EndsWith("QueryUseCase", StringComparison.Ordinal))
            .ToList();

        queryUseCases.Should().NotBeEmpty();

        var invalidUseCases = queryUseCases
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(p =>
                    p.ParameterType.Namespace?.StartsWith("Bud.Server.Application.Abstractions", StringComparison.Ordinal) == true &&
                    p.ParameterType.Name.EndsWith("CommandService", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        invalidUseCases.Should().BeEmpty("query use cases não devem depender de portas command");
    }

    [Fact]
    public void UseCases_ShouldNotDependOnCompositeService_WhenCommandAndQueryPortsExist()
    {
        var abstractionsNamespace = "Bud.Server.Application.Abstractions";
        var allTypes = ServerAssembly.GetTypes();

        var compositeServices = allTypes
            .Where(t =>
                t.IsInterface &&
                t.Namespace == abstractionsNamespace &&
                t.Name.StartsWith('I') &&
                t.Name.EndsWith("Service", StringComparison.Ordinal) &&
                !t.Name.EndsWith("CommandService", StringComparison.Ordinal) &&
                !t.Name.EndsWith("QueryService", StringComparison.Ordinal))
            .ToList();

        var compositesWithSplitPorts = compositeServices
            .Where(composite =>
            {
                var baseName = composite.Name[..^"Service".Length];
                return allTypes.Any(t => t.Namespace == abstractionsNamespace && t.Name == $"{baseName}CommandService") &&
                       allTypes.Any(t => t.Namespace == abstractionsNamespace && t.Name == $"{baseName}QueryService");
            })
            .ToList();

        compositesWithSplitPorts.Should().NotBeEmpty();

        var useCaseTypes = allTypes
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.Namespace?.StartsWith("Bud.Server.Application", StringComparison.Ordinal) == true &&
                t.Name.EndsWith("UseCase", StringComparison.Ordinal))
            .ToList();

        var compositeSet = compositesWithSplitPorts.ToHashSet();

        var invalidUseCases = useCaseTypes
            .Where(type => type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Any(p => compositeSet.Contains(p.ParameterType)))
            .Select(t => t.FullName)
            .ToList();

        invalidUseCases.Should().BeEmpty("use cases devem depender de portas command/query quando essas portas já existirem para o domínio");
    }

    [Fact]
    public void CompositeServices_ShouldBeExplicitlyApproved()
    {
        var abstractionsNamespace = "Bud.Server.Application.Abstractions";
        var allTypes = ServerAssembly.GetTypes();
        var approvedCompositeServices = LoadApprovedCompositeServices();

        var compositeServices = allTypes
            .Where(t =>
                t.IsInterface &&
                t.Namespace == abstractionsNamespace &&
                t.Name.StartsWith('I') &&
                t.Name.EndsWith("Service", StringComparison.Ordinal) &&
                !t.Name.EndsWith("CommandService", StringComparison.Ordinal) &&
                !t.Name.EndsWith("QueryService", StringComparison.Ordinal))
            .Where(t =>
            {
                var baseInterfaces = t.GetInterfaces();
                return baseInterfaces.Any(i => i.Name.EndsWith("CommandService", StringComparison.Ordinal)) &&
                       baseInterfaces.Any(i => i.Name.EndsWith("QueryService", StringComparison.Ordinal));
            })
            .Select(t => t.Name)
            .OrderBy(name => name)
            .ToList();

        compositeServices.Should().NotBeEmpty();

        var unexpectedComposites = compositeServices
            .Where(name => !approvedCompositeServices.Contains(name))
            .ToList();

        unexpectedComposites.Should().BeEmpty(
            "novos serviços compostos exigem decisão arquitetural explícita (atualize ADR e allowlist de serviços compostos)");
    }

    [Fact]
    public void CompositeServicesAllowlist_ShouldNotContainOrphanEntries()
    {
        var abstractionsNamespace = "Bud.Server.Application.Abstractions";
        var allTypes = ServerAssembly.GetTypes();
        var approvedCompositeServices = LoadApprovedCompositeServices();

        var compositeServices = allTypes
            .Where(t =>
                t.IsInterface &&
                t.Namespace == abstractionsNamespace &&
                t.Name.StartsWith('I') &&
                t.Name.EndsWith("Service", StringComparison.Ordinal) &&
                !t.Name.EndsWith("CommandService", StringComparison.Ordinal) &&
                !t.Name.EndsWith("QueryService", StringComparison.Ordinal))
            .Where(t =>
            {
                var baseInterfaces = t.GetInterfaces();
                return baseInterfaces.Any(i => i.Name.EndsWith("CommandService", StringComparison.Ordinal)) &&
                       baseInterfaces.Any(i => i.Name.EndsWith("QueryService", StringComparison.Ordinal));
            })
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        var orphanAllowlistEntries = approvedCompositeServices
            .Where(name => !compositeServices.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        orphanAllowlistEntries.Should().BeEmpty(
            "a allowlist não deve conter entradas órfãs; remova entradas inexistentes ou ajuste o contrato composto correspondente");
    }

    [Fact]
    public void CompositeServicesAllowlist_ShouldBeUniqueAndSorted()
    {
        var entries = LoadApprovedCompositeServiceEntries();
        entries.Should().NotBeEmpty();

        var duplicateEntries = entries
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        duplicateEntries.Should().BeEmpty("a allowlist de serviços compostos não deve conter entradas duplicadas");

        var sortedEntries = entries
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        entries.Should().Equal(sortedEntries, "a allowlist de serviços compostos deve estar ordenada alfabeticamente");
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
    public void DomainEvents_MustHaveAtLeastOneSubscriber()
    {
        var domainEventTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IDomainEvent)))
            .ToList();

        domainEventTypes.Should().NotBeEmpty();

        var subscriberTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        var orphanEvents = domainEventTypes
            .Where(eventType =>
            {
                var expectedInterface = typeof(IDomainEventSubscriber<>).MakeGenericType(eventType);
                return !subscriberTypes.Any(s => expectedInterface.IsAssignableFrom(s));
            })
            .Select(t => t.FullName)
            .ToList();

        orphanEvents.Should().BeEmpty(
            "todo domain event deve ter ao menos um IDomainEventSubscriber<TEvent> registrado");
    }

    [Fact]
    public void DomainEventSubscribers_MustBeRegisteredInDI()
    {
        var subscriberTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventSubscriber<>)))
            .ToList();

        subscriberTypes.Should().NotBeEmpty();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBudApplication();

        var registeredImplementations = services
            .Select(sd => sd.ImplementationType)
            .Where(t => t is not null)
            .ToHashSet();

        var unregistered = subscriberTypes
            .Where(t => !registeredImplementations.Contains(t))
            .Select(t => t.FullName)
            .ToList();

        unregistered.Should().BeEmpty(
            "todo IDomainEventSubscriber<> concreto deve estar registrado no DI via AddBudApplication()");
    }

    [Fact]
    public void DomainEvents_MustBeSealedRecords()
    {
        var domainEventTypes = ServerAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IDomainEvent)))
            .ToList();

        domainEventTypes.Should().NotBeEmpty();

        var nonSealed = domainEventTypes
            .Where(t => !t.IsSealed)
            .Select(t => t.FullName)
            .ToList();

        nonSealed.Should().BeEmpty(
            "domain events devem ser sealed para garantir imutabilidade e serialização correta no outbox");

        var nonRecord = domainEventTypes
            .Where(t => t.GetMethod("<Clone>$") is null)
            .Select(t => t.FullName)
            .ToList();

        nonRecord.Should().BeEmpty(
            "domain events devem ser records para garantir value equality e serialização correta no outbox");
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
            [typeof(MissionTemplate)] = ["Create", "UpdateBasics", "SetActive"]
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

    private static HashSet<string> LoadApprovedCompositeServices()
        => LoadApprovedCompositeServiceEntries().ToHashSet(StringComparer.Ordinal);

    private static List<string> LoadApprovedCompositeServiceEntries()
    {
        var repositoryRoot = FindRepositoryRoot();
        var allowlistPath = Path.Combine(
            repositoryRoot,
            CompositeServiceAllowlistRelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(allowlistPath))
        {
            throw new InvalidOperationException($"Arquivo de allowlist não encontrado: {allowlistPath}");
        }

        var names = File.ReadAllLines(allowlistPath)
            .Select(ParseAllowlistEntry)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        if (names.Count == 0)
        {
            throw new InvalidOperationException($"Arquivo de allowlist sem entradas válidas: {allowlistPath}");
        }

        return names;
    }

    private static string? ParseAllowlistEntry(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
        {
            return null;
        }

        if (trimmed.StartsWith('-') || trimmed.StartsWith('*'))
        {
            trimmed = trimmed[1..].TrimStart();
        }

        string candidate;
        if (trimmed.StartsWith('`'))
        {
            var closeTickIndex = trimmed.IndexOf('`', 1);
            candidate = closeTickIndex > 1 ? trimmed[1..closeTickIndex] : trimmed.Trim('`');
        }
        else
        {
            candidate = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        }

        candidate = candidate.TrimEnd('.', ',', ';', ':');

        return candidate.StartsWith('I') && candidate.EndsWith("Service", StringComparison.Ordinal)
            ? candidate
            : null;
    }

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
