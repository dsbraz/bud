using Bud.Server.Domain.Repositories;
using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class UpdateCollaboratorValidatorTests
{
    [Fact]
    public async Task Validate_WithInvalidLeader_ShouldFail()
    {
        var collaboratorRepository = new Mock<ICollaboratorRepository>();
        collaboratorRepository
            .Setup(x => x.IsValidLeaderAsync(It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var validator = new PatchCollaboratorValidator(collaboratorRepository.Object);
        var request = new PatchCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            LeaderId = Guid.NewGuid()
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("LeaderId"));
    }
}
