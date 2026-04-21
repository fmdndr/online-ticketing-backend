using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;
using Payment.API.Entities;
using Shared.Common.DTOs;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _dbContext;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentDbContext dbContext, ILogger<PaymentController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentRecord>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PaymentRecord>>>> GetPayments()
    {
        var payments = await _dbContext.Payments
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(ApiResponse<List<PaymentRecord>>.Success(payments));
    }

    [HttpGet("order/{orderId}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaymentRecord>>> GetPaymentByOrderId(string orderId)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (payment == null)
        {
            return NotFound(ApiResponse<PaymentRecord>.Fail($"Payment for order '{orderId}' not found."));
        }

        return Ok(ApiResponse<PaymentRecord>.Success(payment));
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentRecord>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PaymentRecord>>>> GetPaymentsByUserId(string userId)
    {
        var payments = await _dbContext.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(ApiResponse<List<PaymentRecord>>.Success(payments));
    }
}
