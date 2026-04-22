using Basket.API.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;
using Shared.Common.Events;
using Shared.Common.Models;

namespace Basket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasketController : ControllerBase
{
    private readonly IBasketRepository _repository;
    private readonly ITopicProducer<TicketReservedEvent> _ticketReservedProducer;
    private readonly ILogger<BasketController> _logger;

    public BasketController(
        IBasketRepository repository,
        ITopicProducer<TicketReservedEvent> ticketReservedProducer,
        ILogger<BasketController> logger)
    {
        _repository = repository;
        _ticketReservedProducer = ticketReservedProducer;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCart>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ShoppingCart>>> GetBasket(string userId)
    {
        var basket = await _repository.GetBasket(userId);
        return Ok(ApiResponse<ShoppingCart>.Success(basket ?? new ShoppingCart { UserId = userId }));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCart>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShoppingCart>>> UpdateBasket([FromBody] ShoppingCart basket)
    {
        // Try to acquire locks for each item in the basket
        foreach (var item in basket.Items)
        {
            var lockAcquired = await _repository.AcquireLock(
                item.EventId, item.TicketTypeName, basket.UserId, TimeSpan.FromMinutes(10));

            if (!lockAcquired)
            {
                _logger.LogWarning("Ticket {TicketType} for event {EventId} is already reserved",
                    item.TicketTypeName, item.EventId);
                return Conflict(ApiResponse<ShoppingCart>.Fail(
                    $"Ticket '{item.TicketTypeName}' for event '{item.EventId}' is already reserved by another user."));
            }
        }

        var result = await _repository.UpdateBasket(basket);
        return Ok(ApiResponse<ShoppingCart>.Success(result!));
    }

    [HttpDelete("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBasket(string userId)
    {
        await _repository.DeleteBasket(userId);
        return Ok(ApiResponse<bool>.Success(true, "Basket deleted successfully"));
    }

    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<string>>> Checkout([FromBody] BasketCheckout checkout)
    {
        var basket = await _repository.GetBasket(checkout.UserId);
        if (basket == null || !basket.Items.Any())
        {
            return BadRequest(ApiResponse<string>.Fail("Basket is empty or not found."));
        }

        // Publish a TicketReservedEvent for each item
        foreach (var item in basket.Items)
        {
            var orderId = Guid.NewGuid().ToString();
            var @event = new TicketReservedEvent
            {
                OrderId = orderId,
                UserId = checkout.UserId,
                EventId = item.EventId,
                TicketTypeName = item.TicketTypeName,
                Quantity = item.Quantity,
                TotalPrice = item.UnitPrice * item.Quantity,
                ReservedAt = DateTime.UtcNow
            };

            await _ticketReservedProducer.Produce(@event);
            _logger.LogInformation("Published TicketReservedEvent for order {OrderId}, event {EventId}",
                orderId, item.EventId);
        }

        return Accepted(ApiResponse<string>.Success("Checkout initiated. Payment is being processed.", "Checkout accepted"));
    }
}
