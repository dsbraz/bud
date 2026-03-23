using System.Text;

namespace Bud.Mcp.Tests.Http;

public sealed class BudApiExceptionTests
{
    [Fact]
    public async Task FromHttpResponseAsync_WithValidationProblem_PreservesFieldErrors()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title": "One or more validation errors occurred.",
                  "status": 400,
                  "errors": {
                    "Name": ["Nome é obrigatório."],
                    "StartDate": ["Data de início é obrigatória."]
                  }
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        var exception = await BudApiException.FromHttpResponseAsync(response);

        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Message.Should().Contain("Name: Nome é obrigatório.");
        exception.Message.Should().Contain("StartDate: Data de início é obrigatória.");
        exception.ValidationErrors.Should().ContainKey("Name");
        exception.ValidationErrors["Name"].Should().ContainSingle().Which.Should().Be("Nome é obrigatório.");
    }
}
