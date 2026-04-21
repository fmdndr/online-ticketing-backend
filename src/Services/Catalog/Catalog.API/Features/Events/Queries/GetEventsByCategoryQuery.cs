using Catalog.API.Entities;
using MediatR;

namespace Catalog.API.Features.Events.Queries;

public record GetEventsByCategoryQuery(string Category) : IRequest<IEnumerable<Event>>;

public class GetEventsByCategoryQueryHandler : IRequestHandler<GetEventsByCategoryQuery, IEnumerable<Event>>
{
    private readonly Repositories.ICatalogRepository _repository;

    public GetEventsByCategoryQueryHandler(Repositories.ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Event>> Handle(GetEventsByCategoryQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetEventsByCategory(request.Category);
    }
}
