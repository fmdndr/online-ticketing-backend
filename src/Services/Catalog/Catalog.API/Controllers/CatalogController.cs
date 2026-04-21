using Catalog.API.Entities;
using Catalog.API.Features.Events.Commands;
using Catalog.API.Features.Events.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(IMediator mediator, ILogger<CatalogController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Event>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Event>>>> GetEvents()
    {
        var events = await _mediator.Send(new GetAllEventsQuery());
        _logger.LogInformation("Retrieved {Count} events", events.Count());
        return Ok(ApiResponse<IEnumerable<Event>>.Success(events));
    }

    [HttpGet("{id}", Name = "GetEvent")]
    [ProducesResponseType(typeof(ApiResponse<Event>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Event>>> GetEventById(string id)
    {
        var @event = await _mediator.Send(new GetEventByIdQuery(id));
        if (@event == null)
        {
            _logger.LogWarning("Event with id {Id} not found", id);
            return NotFound(ApiResponse<Event>.Fail($"Event with id '{id}' not found."));
        }
        return Ok(ApiResponse<Event>.Success(@event));
    }

    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Event>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Event>>>> GetEventsByCategory(string category)
    {
        var events = await _mediator.Send(new GetEventsByCategoryQuery(category));
        return Ok(ApiResponse<IEnumerable<Event>>.Success(events));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Event>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Event>>> CreateEvent([FromBody] CreateEventCommand command)
    {
        var @event = await _mediator.Send(command);
        _logger.LogInformation("Event created with id {Id}", @event.Id);
        return CreatedAtRoute("GetEvent", new { id = @event.Id }, ApiResponse<Event>.Success(@event, "Event created successfully"));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateEvent([FromBody] UpdateEventCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result)
        {
            _logger.LogWarning("Event with id {Id} not found for update", command.Id);
            return NotFound(ApiResponse<bool>.Fail("Event not found."));
        }
        return Ok(ApiResponse<bool>.Success(true, "Event updated successfully"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEvent(string id)
    {
        var result = await _mediator.Send(new DeleteEventCommand(id));
        if (!result)
        {
            _logger.LogWarning("Event with id {Id} not found for deletion", id);
            return NotFound(ApiResponse<bool>.Fail("Event not found."));
        }
        _logger.LogInformation("Event with id {Id} deleted", id);
        return Ok(ApiResponse<bool>.Success(true, "Event deleted successfully"));
    }
}
