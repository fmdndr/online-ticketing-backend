namespace Shared.Common.DTOs;

public record EventDto(
    string Id,
    string Name,
    string Description,
    string Category,
    string ImageUrl,
    string Venue,
    DateTime Date,
    List<TicketTypeDto> TicketTypes
);

public record TicketTypeDto(
    string Name,
    decimal Price,
    int AvailableQuantity
);
