using Catalog.API.Entities;
using Catalog.API.Repositories;
using MediatR;

namespace Catalog.API.Features.Events.Commands;

public record UpdateEventCommand : IRequest<bool>
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string Venue { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public List<TicketType> TicketTypes { get; init; } = new();
}

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, bool>
{
    private readonly ICatalogRepository _repository;

    public UpdateEventCommandHandler(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = new Event
        {
            Id = request.Id,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            ImageUrl = request.ImageUrl,
            Venue = request.Venue,
            Date = request.Date,
            TicketTypes = request.TicketTypes
        };

        return await _repository.UpdateEvent(@event);
    }
}
