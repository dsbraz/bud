using Bud.Application.Common;

namespace Bud.Application.Templates;

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
