using Bud.Server.Application.Ports;
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

        var validator = new UpdateCollaboratorValidator(collaboratorRepository.Object);
        var request = new UpdateCollaboratorRequest
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            LeaderId = Guid.NewGuid()
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaderId");
    }
}
