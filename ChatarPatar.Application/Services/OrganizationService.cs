using AutoMapper;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Application.Services;

internal class OrganizationService : IOrganizationService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;

    public OrganizationService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
    }
}
