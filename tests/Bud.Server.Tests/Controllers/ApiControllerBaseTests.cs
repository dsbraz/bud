using Bud.Server.Controllers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Bud.Server.Application.Common;

namespace Bud.Server.Tests.Controllers;

public sealed class ApiControllerBaseTests
{
    private sealed class TestController : ApiControllerBase
    {
        public sealed class DummyEntity
        {
            public Guid OrganizationId { get; set; }
        }

        public ActionResult CallValidationProblemFrom(ValidationResult validationResult)
            => ValidationProblemFrom(validationResult);

        public ActionResult CallFromResult(Result result)
            => FromResult(result, () => Ok());

        public ActionResult<string> CallFromResult(Result<string> result)
            => FromResult(result, value => Ok(value));

        public ActionResult<string> CallFromResultOk(Result<string> result)
            => FromResultOk(result);

        public ObjectResult CallForbiddenProblem(string detail)
            => ForbiddenProblem(detail);

        public ActionResult? CallValidatePagination(int page, int pageSize, int maxPageSize = 100)
            => ValidatePagination(page, pageSize, maxPageSize);

        public Task<ActionResult?> CallEnsureAuthorizedAsync(
            IAuthorizationService authorizationService,
            object resource,
            string policyName,
            string forbiddenDetail)
            => EnsureAuthorizedAsync(authorizationService, resource, policyName, forbiddenDetail);

        public (List<Guid>? Values, ActionResult? Failure) CallParseGuidCsv(string? csv, string parameterName)
        {
            var result = ParseGuidCsv(csv, parameterName);
            return (result.Values, result.Failure);
        }

        public (string? Value, ActionResult? Failure) CallValidateAndNormalizeSearch(
            string? search,
            int maxLength = 200,
            string parameterName = "search")
        {
            var result = ValidateAndNormalizeSearch(search, maxLength, parameterName);
            return (result.Value, result.Failure);
        }

        public async Task<(DummyEntity? Entity, ActionResult? Failure)> CallLoadEntityAndEnsureAuthorizedAsync(
            Func<CancellationToken, Task<DummyEntity?>> loadEntityAsync,
            IAuthorizationService authorizationService,
            string policyName,
            string notFoundDetail,
            string forbiddenDetail,
            CancellationToken cancellationToken = default)
        {
            var result = await LoadEntityAndEnsureAuthorizedAsync(
                loadEntityAsync,
                entity => new object(),
                authorizationService,
                policyName,
                notFoundDetail,
                forbiddenDetail,
                cancellationToken);
            return (result.Entity, result.Failure);
        }
    }

    [Fact]
    public void ValidationProblemFrom_WhenInvalid_ReturnsBadRequestWithValidationDetails()
    {
        // Arrange
        var controller = new TestController();
        var validationResult = new ValidationResult([
            new ValidationFailure("Name", "Nome é obrigatório.")
        ]);

        // Act
        var actionResult = controller.CallValidationProblemFrom(validationResult);

        // Assert
        var objectResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("Name");
        problem.Errors["Name"].Should().Contain("Nome é obrigatório.");
    }

    [Fact]
    public void FromResult_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var actionResult = controller.CallFromResult(Result.NotFound("Registro não encontrado."));

        // Assert
        var objectResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("Registro não encontrado.");
    }

    [Fact]
    public void FromResult_WhenConflict_ReturnsConflict()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var actionResult = controller.CallFromResult(
            Result.Failure("Conflito de dados.", ErrorType.Conflict));

        // Assert
        var objectResult = actionResult.Should().BeOfType<ConflictObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("Conflito de dados.");
    }

    [Fact]
    public void FromResult_WhenForbidden_ReturnsForbiddenProblem()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var actionResult = controller.CallFromResult(Result.Forbidden("Acesso negado ao recurso."));

        // Assert
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Be("Acesso negado");
        problem.Detail.Should().Be("Acesso negado ao recurso.");
    }

    [Fact]
    public void FromResultGeneric_WhenSuccess_ReturnsOkWithPayload()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var actionResult = controller.CallFromResult(Result<string>.Success("ok"));

        // Assert
        var objectResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.Value.Should().Be("ok");
    }

    [Fact]
    public void FromResultOk_WhenSuccess_ReturnsOkWithPayload()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var actionResult = controller.CallFromResultOk(Result<string>.Success("ok"));

        // Assert
        var objectResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.Value.Should().Be("ok");
    }

    [Fact]
    public void FromResultOk_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var actionResult = controller.CallFromResultOk(Result<string>.NotFound("Registro não encontrado."));

        // Assert
        var objectResult = actionResult.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("Registro não encontrado.");
    }

    [Fact]
    public void ValidatePagination_WhenValid_ReturnsNull()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.CallValidatePagination(1, 10);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidatePagination_WhenPageIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.CallValidatePagination(0, 10);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("O parâmetro 'page' deve ser maior ou igual a 1.");
    }

    [Fact]
    public void ValidatePagination_WhenPageSizeIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.CallValidatePagination(1, 101);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("O parâmetro 'pageSize' deve estar entre 1 e 100.");
    }

    [Fact]
    public void ParseGuidCsv_WhenValid_ReturnsParsedValues()
    {
        // Arrange
        var controller = new TestController();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        // Act
        var result = controller.CallParseGuidCsv($"{first}, {second}", "ids");

        // Assert
        result.Failure.Should().BeNull();
        result.Values.Should().BeEquivalentTo([first, second]);
    }

    [Fact]
    public void ParseGuidCsv_WhenInvalidValue_ReturnsBadRequest()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.CallParseGuidCsv("abc", "ids");

        // Assert
        result.Values.Should().BeNull();
        var badRequest = result.Failure.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("O parâmetro 'ids' contém valores inválidos. Informe GUIDs separados por vírgula.");
    }

    [Fact]
    public void ValidateAndNormalizeSearch_WhenEmpty_ReturnsNullWithoutFailure()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.CallValidateAndNormalizeSearch("   ");

        // Assert
        result.Value.Should().BeNull();
        result.Failure.Should().BeNull();
    }

    [Fact]
    public void ValidateAndNormalizeSearch_WhenValid_ReturnsTrimmedValue()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.CallValidateAndNormalizeSearch("  produto  ");

        // Assert
        result.Failure.Should().BeNull();
        result.Value.Should().Be("produto");
    }

    [Fact]
    public void ValidateAndNormalizeSearch_WhenTooLong_ReturnsBadRequest()
    {
        // Arrange
        var controller = new TestController();
        var tooLong = new string('a', 201);

        // Act
        var result = controller.CallValidateAndNormalizeSearch(tooLong);

        // Assert
        result.Value.Should().BeNull();
        var badRequest = result.Failure.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("O parâmetro 'search' deve ter no máximo 200 caracteres.");
    }

    [Fact]
    public void ForbiddenProblem_ReturnsStandardForbiddenProblemDetails()
    {
        // Arrange
        var controller = new TestController();

        // Act
        var result = controller.CallForbiddenProblem("Você não pode executar esta ação.");

        // Assert
        result.StatusCode.Should().Be(403);
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Be("Acesso negado");
        problem.Detail.Should().Be("Você não pode executar esta ação.");
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_WhenAuthorized_ReturnsNull()
    {
        // Arrange
        var controller = new TestController();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(controller.User, It.IsAny<object>(), "policy"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await controller.CallEnsureAuthorizedAsync(
            authorizationService.Object,
            new object(),
            "policy",
            "mensagem");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_WhenUnauthorized_ReturnsForbiddenProblem()
    {
        // Arrange
        var controller = new TestController();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(controller.User, It.IsAny<object>(), "policy"))
            .ReturnsAsync(AuthorizationResult.Failed());

        // Act
        var result = await controller.CallEnsureAuthorizedAsync(
            authorizationService.Object,
            new object(),
            "policy",
            "Sem permissão.");

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("Sem permissão.");
    }

    [Fact]
    public async Task LoadEntityAndEnsureAuthorizedAsync_WhenEntityNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var controller = new TestController();
        var authorizationService = new Mock<IAuthorizationService>(MockBehavior.Strict);

        // Act
        var result = await controller.CallLoadEntityAndEnsureAuthorizedAsync(
            _ => Task.FromResult<TestController.DummyEntity?>(null),
            authorizationService.Object,
            "policy",
            "Registro não encontrado.",
            "Sem permissão.");

        // Assert
        result.Entity.Should().BeNull();
        var notFound = result.Failure.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = notFound.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("Registro não encontrado.");
    }

    [Fact]
    public async Task LoadEntityAndEnsureAuthorizedAsync_WhenUnauthorized_ReturnsForbiddenFailure()
    {
        // Arrange
        var controller = new TestController();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(controller.User, It.IsAny<object>(), "policy"))
            .ReturnsAsync(AuthorizationResult.Failed());

        // Act
        var result = await controller.CallLoadEntityAndEnsureAuthorizedAsync(
            _ => Task.FromResult<TestController.DummyEntity?>(new TestController.DummyEntity()),
            authorizationService.Object,
            "policy",
            "Registro não encontrado.",
            "Sem permissão.");

        // Assert
        result.Entity.Should().BeNull();
        var forbidden = result.Failure.Should().BeOfType<ObjectResult>().Subject;
        forbidden.StatusCode.Should().Be(403);
        var problem = forbidden.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Be("Sem permissão.");
    }

    [Fact]
    public async Task LoadEntityAndEnsureAuthorizedAsync_WhenAuthorized_ReturnsEntity()
    {
        // Arrange
        var controller = new TestController();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(controller.User, It.IsAny<object>(), "policy"))
            .ReturnsAsync(AuthorizationResult.Success());
        var entity = new TestController.DummyEntity { OrganizationId = Guid.NewGuid() };

        // Act
        var result = await controller.CallLoadEntityAndEnsureAuthorizedAsync(
            _ => Task.FromResult<TestController.DummyEntity?>(entity),
            authorizationService.Object,
            "policy",
            "Registro não encontrado.",
            "Sem permissão.");

        // Assert
        result.Failure.Should().BeNull();
        result.Entity.Should().NotBeNull();
        result.Entity!.OrganizationId.Should().Be(entity.OrganizationId);
    }
}
