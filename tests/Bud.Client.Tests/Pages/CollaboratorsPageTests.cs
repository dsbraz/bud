using Bud.Client.Pages;
using Bud.Shared.Domain;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Pages;

public sealed class CollaboratorsPageTests
{
    [Theory]
    [InlineData(CollaboratorRole.Leader, "Lider")]
    [InlineData(CollaboratorRole.IndividualContributor, "Contribuidor individual")]
    public void GetRoleLabel_ShouldReturnExpectedPortugueseLabel(CollaboratorRole role, string expected)
    {
        var method = typeof(Collaborators).GetMethod(
            "GetRoleLabel",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

        var result = method.Invoke(null, [role]) as string;

        result.Should().Be(expected);
    }
}
