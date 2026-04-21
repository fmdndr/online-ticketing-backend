using Catalog.API.Entities;
using MediatR;

namespace Catalog.API.Features.Events.Queries;

public record GetAllEventsQuery : IRequest<IEnumerable<Event>>;

public class GetAllEventsQueryHandler : IRequestHandler<GetAllEventsQuery, IEnumerable<Event>>
{
    private readonly Repositories.ICatalogRepository _repository;

    public GetAllEventsQueryHandler(Repositories.ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Event>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetEvents();
    }
}
