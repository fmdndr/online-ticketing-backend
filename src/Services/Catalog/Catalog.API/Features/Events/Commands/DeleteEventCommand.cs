using Catalog.API.Repositories;
using MediatR;

namespace Catalog.API.Features.Events.Commands;

public record DeleteEventCommand(string Id) : IRequest<bool>;

public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, bool>
{
    private readonly ICatalogRepository _repository;

    public DeleteEventCommandHandler(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        return await _repository.DeleteEvent(request.Id);
    }
}
