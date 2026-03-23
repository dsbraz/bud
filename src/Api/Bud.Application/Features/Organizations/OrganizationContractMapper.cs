using Bud.Application.Features.Collaborators;

namespace Bud.Application.Features.Organizations;

public static class OrganizationContractMapper
{
    public static OrganizationResponse ToResponse(this Organization source)
    {
        return new OrganizationResponse
        {
            Id = source.Id,
            Name = source.Name,
            OwnerId = source.OwnerId,
            Owner = source.Owner?.ToCollaboratorResponse()
        };
    }
}
