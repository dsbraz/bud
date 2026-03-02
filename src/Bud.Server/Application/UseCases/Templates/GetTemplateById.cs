using Bud.Server.Application.Common;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Templates;

public sealed class GetTemplateById(ITemplateRepository templateRepository)
{
    public async Task<Result<Template>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdReadOnlyAsync(id, cancellationToken);
        return template is null
            ? Result<Template>.NotFound(UserErrorMessages.TemplateNotFound)
            : Result<Template>.Success(template);
    }
}
