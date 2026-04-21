using Catalog.API.Entities;
using MediatR;

namespace Catalog.API.Features.Events.Queries;

public record GetEventByIdQuery(string Id) : IRequest<Event?>;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Event?>
{
    private readonly Repositories.ICatalogRepository _repository;

    public GetEventByIdQueryHandler(Repositories.ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<Event?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetEvent(request.Id);
    }
}
